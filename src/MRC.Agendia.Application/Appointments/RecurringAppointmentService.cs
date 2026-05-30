using AutoMapper;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Appointments.Recurrence;
using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Appointments
{
    public class RecurringAppointmentService : IRecurringAppointmentService
    {
        private const string MonthWithoutDayCode = "RECURRENCE_MONTH_WITHOUT_DAY";
        private const string SeriesMoveTargetCollisionCode = "SERIES_MOVE_TARGET_COLLISION";

        private readonly IServiceRepository _serviceRepository;
        private readonly IAppointmentRepository _repository;
        private readonly IAppointmentSchedulingValidator _schedulingValidator;
        private readonly IBookingConcurrencyGuard _bookingGuard;
        private readonly IAuditLogger _auditLogger;
        private readonly IClock _clock;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RecurringAppointmentService(
            IServiceRepository serviceRepository,
            IAppointmentRepository repository,
            IAppointmentSchedulingValidator schedulingValidator,
            IBookingConcurrencyGuard bookingGuard,
            IAuditLogger auditLogger,
            IClock clock,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _serviceRepository = serviceRepository;
            _repository = repository;
            _schedulingValidator = schedulingValidator;
            _bookingGuard = bookingGuard;
            _auditLogger = auditLogger;
            _clock = clock;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        /// <inheritdoc />
        public async Task<AppointmentSeriesResultDto> CreateSeriesAsync(CreateAppointmentSeriesDto dto, CancellationToken cancellationToken = default)
        {
            // The service is loaded up front: its duration sets each occurrence's
            // end, and a missing/invalid service is a request-level error (404) for
            // the whole series, not a per-occurrence skip.
            var service = await _serviceRepository.GetByIdAsync(dto.ServiceId, cancellationToken)
                ?? throw new ServiceNotFoundException(dto.ServiceId);

            // Never generate past dates. The per-occurrence validator still guards
            // "today but the start time already passed" (reported as a skip).
            var today = DateOnly.FromDateTime(_clock.BusinessNow);
            var from = dto.StartDate < today ? today : dto.StartDate;

            var expansion = RecurrenceExpander.Expand(
                dto.Frequency, dto.Interval, dto.DaysOfWeek, dto.DayOfMonth, from, dto.UntilDate);

            var seriesId = Guid.NewGuid();
            var created = new List<Appointment>();
            var skipped = new List<SkippedOccurrenceDto>();

            foreach (var shortMonth in expansion.ShortMonths)
            {
                skipped.Add(new SkippedOccurrenceDto(
                    shortMonth, MonthWithoutDayCode,
                    $"El mes {shortMonth:yyyy-MM} no tiene el dia {dto.DayOfMonth}."));
            }

            foreach (var date in expansion.Dates)
            {
                var start = date.ToDateTime(dto.StartTime);
                var end = start.AddMinutes(service.DurationMinutes);

                try
                {
                    // Same validate-then-insert critical section as a single booking,
                    // serialized per employee/day so a series cannot overbook a slot.
                    var entity = await _bookingGuard.ExecuteSerializedAsync(dto.EmployeeId, date, async () =>
                    {
                        await _schedulingValidator.EnsureValidAsync(
                            appointmentId: null,
                            clientId: dto.ClientId,
                            employeeId: dto.EmployeeId,
                            serviceId: dto.ServiceId,
                            startDate: start,
                            endDate: end,
                            cancellationToken: cancellationToken);

                        var appointment = new Appointment
                        {
                            ClientId = dto.ClientId,
                            EmployeeId = dto.EmployeeId,
                            ServiceId = dto.ServiceId,
                            StartDate = start,
                            EndDate = end,
                            Status = AppointmentStatus.Pending,
                            Notes = dto.Notes,
                            SeriesId = seriesId
                        };
                        await _repository.AddAsync(appointment, cancellationToken);
                        await _unitOfWork.Save(cancellationToken);
                        return appointment;
                    }, cancellationToken);

                    created.Add(entity);
                }
                catch (DomainException ex) when (IsDateSpecific(ex))
                {
                    skipped.Add(new SkippedOccurrenceDto(date, ex.Code, ex.Message));
                }
            }

            // Audit even a skip-only outcome: the staff acted on the series and every
            // occurrence being rejected (closed/full/past) must still leave a trace.
            if (created.Count > 0 || skipped.Count > 0)
            {
                await _auditLogger.LogAsync(
                    AuditActions.AppointmentSeriesCreated, "AppointmentSeries", seriesId.ToString(),
                    new { created = created.Count, skipped = skipped.Count, dto.ClientId, dto.EmployeeId, dto.ServiceId },
                    cancellationToken);
            }

            return new AppointmentSeriesResultDto(seriesId, ToDtosByDate(created), OrderByDate(skipped));
        }

        /// <inheritdoc />
        public async Task<AppointmentSeriesCountResultDto> CancelSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default)
        {
            var appointments = await GetSeriesOrThrowAsync(seriesId, cancellationToken);

            var now = _clock.BusinessNow;
            var affected = 0;
            foreach (var appointment in appointments)
            {
                if (appointment.StartDate >= now && OccupiesSlot(appointment.Status))
                {
                    appointment.Status = AppointmentStatus.Cancelled;
                    _repository.Update(appointment);
                    affected++;
                }
            }

            if (affected > 0)
            {
                await _unitOfWork.Save(cancellationToken);
                await _auditLogger.LogAsync(
                    AuditActions.AppointmentSeriesCancelled, "AppointmentSeries", seriesId.ToString(),
                    new { cancelled = affected }, cancellationToken);
            }

            return new AppointmentSeriesCountResultDto(seriesId, affected);
        }

        /// <inheritdoc />
        public async Task<AppointmentSeriesCountResultDto> DeleteSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default)
        {
            var appointments = await GetSeriesOrThrowAsync(seriesId, cancellationToken);

            foreach (var appointment in appointments)
                _repository.Delete(appointment); // interceptor converts this to a soft delete

            await _unitOfWork.Save(cancellationToken);
            await _auditLogger.LogAsync(
                AuditActions.AppointmentSeriesDeleted, "AppointmentSeries", seriesId.ToString(),
                new { deleted = appointments.Count }, cancellationToken);

            return new AppointmentSeriesCountResultDto(seriesId, appointments.Count);
        }

        /// <inheritdoc />
        public async Task<MoveAppointmentSeriesResultDto> MoveSeriesAsync(Guid seriesId, MoveAppointmentSeriesDto dto, CancellationToken cancellationToken = default)
        {
            var appointments = await GetSeriesOrThrowAsync(seriesId, cancellationToken);

            var now = _clock.BusinessNow;
            var moved = new List<Appointment>();
            var skipped = new List<SkippedOccurrenceDto>();

            foreach (var appointment in appointments)
            {
                // Only future, still-active occurrences move; history is left intact.
                if (appointment.StartDate < now || !OccupiesSlot(appointment.Status))
                    continue;

                var originalDate = DateOnly.FromDateTime(appointment.StartDate);
                var duration = appointment.EndDate - appointment.StartDate;
                var newDate = originalDate.AddDays(dto.DayShift);
                var newTime = dto.NewStartTime ?? TimeOnly.FromDateTime(appointment.StartDate);
                var newStart = newDate.ToDateTime(newTime);
                var newEnd = newStart + duration;

                try
                {
                    await _bookingGuard.ExecuteSerializedAsync(appointment.EmployeeId, newDate, async () =>
                    {
                        await _schedulingValidator.EnsureValidAsync(
                            appointmentId: appointment.Id,
                            clientId: appointment.ClientId,
                            employeeId: appointment.EmployeeId,
                            serviceId: appointment.ServiceId,
                            startDate: newStart,
                            endDate: newEnd,
                            cancellationToken: cancellationToken);

                        appointment.StartDate = newStart;
                        appointment.EndDate = newEnd;
                        appointment.ReminderSentAt = null; // re-arm the 24h reminder for the new time
                        _repository.Update(appointment);
                        await _unitOfWork.Save(cancellationToken);
                    }, cancellationToken);

                    moved.Add(appointment);
                }
                catch (AppointmentConflictException ex)
                {
                    // A capacity conflict whose target overlaps another still-active
                    // occurrence of THIS series is the series colliding with itself (the
                    // shift landed it on a sibling). Surface that with a distinct code so
                    // the collapse is not lost inside a generic external conflict. This is
                    // an order-dependent signal: with a uniform shift the final state has
                    // no real overlap, so it flags an occurrence stranded because a sibling
                    // had not been moved out of the way yet, not a persistent double-book.
                    var collidesWithSibling = OverlapsActiveSibling(appointments, appointment, newStart, newEnd);
                    skipped.Add(new SkippedOccurrenceDto(
                        originalDate,
                        collidesWithSibling ? SeriesMoveTargetCollisionCode : ex.Code,
                        collidesWithSibling
                            ? $"La ocurrencia del {originalDate:yyyy-MM-dd} choca con otra cita de la misma serie en la fecha destino."
                            : ex.Message));
                }
                catch (DomainException ex) when (IsDateSpecific(ex))
                {
                    skipped.Add(new SkippedOccurrenceDto(originalDate, ex.Code, ex.Message));
                }
            }

            // Audit even a skip-only outcome: a move that rejected every occurrence is
            // still a staff action on the series and must leave a trace.
            if (moved.Count > 0 || skipped.Count > 0)
            {
                await _auditLogger.LogAsync(
                    AuditActions.AppointmentSeriesMoved, "AppointmentSeries", seriesId.ToString(),
                    new { moved = moved.Count, skipped = skipped.Count, dto.DayShift, newTime = dto.NewStartTime?.ToString() },
                    cancellationToken);
            }

            return new MoveAppointmentSeriesResultDto(seriesId, ToDtosByDate(moved), OrderByDate(skipped));
        }

        private async Task<IReadOnlyList<Appointment>> GetSeriesOrThrowAsync(Guid seriesId, CancellationToken cancellationToken)
        {
            var appointments = await _repository.GetBySeriesIdAsync(seriesId, cancellationToken);
            if (appointments.Count == 0)
                throw new AppointmentSeriesNotFoundException(seriesId);
            return appointments;
        }

        // Date-specific failures (closed day, capacity full, slot in the past) are
        // skipped per occurrence; request-level failures (not found, inactive
        // employee, business mismatch) propagate and fail the whole request.
        private static bool IsDateSpecific(DomainException ex)
            => ex is AppointmentOutsideScheduleException
               or AppointmentConflictException
               or InvalidAppointmentTimeException;

        // True when the moved occurrence's new slot overlaps another still-active
        // occurrence of the same series for the same employee. Uses each sibling's
        // current position, which already reflects earlier moves in this run.
        private static bool OverlapsActiveSibling(
            IEnumerable<Appointment> series, Appointment moving, DateTime newStart, DateTime newEnd)
            => series.Any(other =>
                other.Id != moving.Id
                && other.EmployeeId == moving.EmployeeId
                && OccupiesSlot(other.Status)
                && newStart < other.EndDate && newEnd > other.StartDate);

        private static bool OccupiesSlot(AppointmentStatus status)
            => status is AppointmentStatus.Pending or AppointmentStatus.Confirmed;

        private List<AppointmentDto> ToDtosByDate(IEnumerable<Appointment> appointments)
            => appointments.OrderBy(a => a.StartDate).Select(a => _mapper.Map<AppointmentDto>(a)).ToList();

        private static List<SkippedOccurrenceDto> OrderByDate(IEnumerable<SkippedOccurrenceDto> skipped)
            => skipped.OrderBy(s => s.Date).ToList();
    }
}
