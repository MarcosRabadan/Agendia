using AutoMapper;
using Microsoft.Extensions.Logging;
using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Application.Availability;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Waitlist.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Events;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Waitlist
{
    public class WaitlistService : IWaitlistService
    {
        private readonly IWaitlistRepository _repository;
        private readonly IAvailabilityService _availabilityService;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IBookingConcurrencyGuard _bookingGuard;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<WaitlistService> _logger;
        private readonly IMapper _mapper;
        private readonly IClock _clock;
        private readonly WaitlistOptions _options;

        public WaitlistService(IWaitlistRepository repository,
                               IAvailabilityService availabilityService,
                               IAppointmentRepository appointmentRepository,
                               IBookingConcurrencyGuard bookingGuard,
                               IUnitOfWork unitOfWork,
                               ILogger<WaitlistService> logger,
                               IMapper mapper,
                               IClock clock,
                               WaitlistOptions options)
        {
            _clock = clock;
            _options = options;
            _repository = repository;
            _availabilityService = availabilityService;
            _appointmentRepository = appointmentRepository;
            _bookingGuard = bookingGuard;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
        }

        /// <inheritdoc />
        public async Task<WaitlistEntryDto> JoinAsync(JoinWaitlistDto dto, string userId, CancellationToken cancellationToken = default)
        {
            // The slot must exist in the schedule and be full (capacity 0). If it
            // still has room, the client should just book directly.
            var capacity = await _availabilityService.GetSlotCapacityAsync(
                dto.BusinessId, dto.Date, dto.StartTime, dto.ServiceId, dto.EmployeeId, cancellationToken);
            if (capacity is null)
                throw new InvalidOperationException("The given slot is not within the business schedule.");
            if (capacity > 0)
                throw new SlotHasCapacityException();

            if (await _repository.ExistsWaitingAsync(
                    userId, dto.BusinessId, dto.ServiceId, dto.Date, dto.StartTime, dto.EmployeeId, cancellationToken))
                throw new DuplicateWaitlistEntryException();

            var entry = new WaitlistEntry
            {
                BusinessId = dto.BusinessId,
                ServiceId = dto.ServiceId,
                ClientUserId = userId,
                EmployeeId = dto.EmployeeId,
                Date = dto.Date,
                StartTime = dto.StartTime,
                Status = WaitlistStatus.Waiting,
                CreatedAt = DateTime.UtcNow,
            };
            await _repository.AddAsync(entry, cancellationToken);
            await _unitOfWork.Save(cancellationToken);

            return _mapper.Map<WaitlistEntryDto>(entry);
        }

        /// <inheritdoc />
        public async Task LeaveAsync(Guid entryId, string userId, CancellationToken cancellationToken = default)
        {
            var entry = await _repository.GetByIdAsync(entryId, cancellationToken)
                ?? throw new WaitlistEntryNotFoundException(entryId);

            if (entry.ClientUserId != userId)
                throw new UnauthorizedAccessException("You can only manage your own waitlist entries.");

            if (entry.Status == WaitlistStatus.Cancelled)
                return; // idempotent

            entry.Status = WaitlistStatus.Cancelled;
            _repository.Update(entry);
            await _unitOfWork.Save(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<WaitlistEntryDto>> GetMineAsync(string userId, CancellationToken cancellationToken = default)
        {
            var entries = await _repository.GetActiveByClientUserIdAsync(userId, cancellationToken);
            return _mapper.Map<List<WaitlistEntryDto>>(entries);
        }

        /// <inheritdoc />
        public async Task ConsumeHoldAsync(string clientUserId,
                                           Guid employeeId,
                                           DateTime startDate,
                                           CancellationToken cancellationToken = default)
        {
            try
            {
                var hold = await _repository.GetActiveHoldForClientAsync(
                    clientUserId, DateOnly.FromDateTime(startDate), TimeOnly.FromDateTime(startDate),
                    employeeId, DateTime.UtcNow, cancellationToken);
                if (hold is null)
                    return;

                // The seat is now taken by their own appointment, so the hold must stop
                // subtracting capacity on top of it.
                hold.Status = WaitlistStatus.Booked;
                hold.HoldUntil = null;
                _repository.Update(hold);
                await _unitOfWork.Save(cancellationToken);
            }
            catch (Exception ex)
            {
                // Best-effort, like the notification: the appointment is already committed
                // and must not be undone because the bookkeeping failed. Logged, not silent.
                _logger.LogWarning(ex,
                    "Failed to consume the waitlist hold of client {ClientUserId} (best-effort, ignored).",
                    clientUserId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyForFreedAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
        {
            // Best-effort: this runs after the cancellation/deletion has been saved,
            // so any failure here must NOT bubble up and fail that operation.
            try
            {
                var appointment = await _appointmentRepository.GetByIdWithDetailsAsync(appointmentId, cancellationToken);
                if (appointment is null)
                    return;

                var date = DateOnly.FromDateTime(appointment.StartDate);
                var businessId = appointment.Employee.BusinessId;
                var serviceId = appointment.ServiceId;
                var employeeId = appointment.EmployeeId;

                // Who this freed window could serve: everybody whose own slot overlaps it, not
                // only whoever asked for the very same start time (#350). Joining the queue is
                // allowed whenever the slot is full, and fullness is measured by overlap, so a
                // 10:00-11:00 class leaves people legitimately waiting at 10:15 and 10:30.
                var (windowEnd, earliestStart) = WaitlistSlotWindow.OverlapBounds(
                    appointment.StartDate, appointment.EndDate, appointment.Service.DurationMinutes);

                // Serialize select-recheck-notify-mark per employee+day (the same key
                // booking uses) so two concurrent cancellations of the same slot cannot
                // pick the same Waiting entry and notify it twice, nor interleave a Leave
                // with the Notified write. On the in-memory test store the guard just runs
                // the action (no shared DB to race against).
                await _bookingGuard.ExecuteSerializedAsync(employeeId, date, async () =>
                {
                    var candidates = await _repository.GetWaitingCandidatesForSlotAsync(
                        businessId, serviceId, date, windowEnd, earliestStart, employeeId,
                        Math.Max(1, _options.NotifyCandidateLimit), cancellationToken);

                    // Walk them FIFO and notify the FIRST ONE THAT FITS, not simply the first.
                    // Overlapping candidates do not all want the same slot: the freed seat may
                    // leave the 10:30 window still blocked by the 11:00 class while 10:00 is now
                    // free. Stopping at the head would starve everyone behind a candidate whose
                    // own slot is still full.
                    foreach (var candidate in candidates)
                    {
                        // Capacity is the authority; the overlap filter above only proposes.
                        // The freed appointment may not have opened a seat at all (employee
                        // MaxConcurrentAppointments > 1), and an "any-employee" entry
                        // (EmployeeId null) only wants a slot genuinely free across the business.
                        // Skip when it is still full (0) or no longer schedulable (null) so we do
                        // not raise a false "there is a spot" alert.
                        var capacity = await _availabilityService.GetSlotCapacityAsync(
                            candidate.BusinessId, candidate.Date, candidate.StartTime,
                            candidate.ServiceId, candidate.EmployeeId, cancellationToken);
                        if (capacity is null or <= 0)
                            continue;

                        // Raise the availability event on the entry and mark it Notified: the
                        // DbContext SaveChanges override enlists the event into the outbox in the
                        // SAME save as the Notified mark, so the slot is never both "notified" with
                        // no event nor announced twice. The consumer resolves the client's contact
                        // from ClientUserId and delivers in the business language.
                        // Priority hold (#268): the slot is theirs until holdUntil, so the
                        // notification is worth something even if they take a few minutes to
                        // react. The availability read and the booking validator both honour
                        // it, and the expiry job moves the queue on if they do not book.
                        var holdUntil = DateTime.UtcNow.AddMinutes(Math.Max(1, _options.HoldMinutes));

                        candidate.RaiseEvent(new WaitlistSlotAvailable(
                            candidate.Id, candidate.BusinessId, candidate.EmployeeId, candidate.ClientUserId,
                            candidate.ServiceId, candidate.Date, candidate.StartTime, holdUntil,
                            appointment.Employee.Business.DefaultLanguage, _clock.TimeZoneId, DateTime.UtcNow));

                        candidate.Status = WaitlistStatus.Notified;
                        candidate.HoldUntil = holdUntil;
                        _repository.Update(candidate);
                        await _unitOfWork.Save(cancellationToken);

                        // One freed seat, one notification: whoever is next stays queued.
                        break;
                    }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                // Best-effort: notifying the waitlist is a non-critical side effect of
                // cancelling/deleting an appointment, so a failure here must never bubble up
                // and fail that (already committed) operation. It is logged instead of
                // swallowed silently, so a persistent problem is diagnosable, not invisible.
                _logger.LogWarning(ex,
                    "Failed to notify the waitlist for appointment {AppointmentId} (best-effort, ignored).",
                    appointmentId);
            }
        }
    }
}
