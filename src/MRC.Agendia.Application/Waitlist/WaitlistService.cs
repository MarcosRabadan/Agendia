using AutoMapper;
using Microsoft.Extensions.Logging;
using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Application.Availability;
using MRC.Agendia.Application.Events;
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
        private readonly IEventPublisher _eventPublisher;
        private readonly IBookingConcurrencyGuard _bookingGuard;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<WaitlistService> _logger;
        private readonly IMapper _mapper;

        public WaitlistService(IWaitlistRepository repository,
                               IAvailabilityService availabilityService,
                               IAppointmentRepository appointmentRepository,
                               IEventPublisher eventPublisher,
                               IBookingConcurrencyGuard bookingGuard,
                               IUnitOfWork unitOfWork,
                               ILogger<WaitlistService> logger,
                               IMapper mapper)
        {
            _repository = repository;
            _availabilityService = availabilityService;
            _appointmentRepository = appointmentRepository;
            _eventPublisher = eventPublisher;
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
                throw new InvalidOperationException("La franja indicada no esta dentro del horario del negocio.");
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
                throw new UnauthorizedAccessException("Solo puedes gestionar tus propias entradas de lista de espera.");

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
                var startTime = TimeOnly.FromDateTime(appointment.StartDate);
                var businessId = appointment.Employee.BusinessId;
                var serviceId = appointment.ServiceId;
                var employeeId = appointment.EmployeeId;

                // Serialize select-recheck-notify-mark per employee+day (the same key
                // booking uses) so two concurrent cancellations of the same slot cannot
                // pick the same Waiting entry and notify it twice, nor interleave a Leave
                // with the Notified write. On the in-memory test store the guard just runs
                // the action (no shared DB to race against).
                await _bookingGuard.ExecuteSerializedAsync(employeeId, date, async () =>
                {
                    var entry = await _repository.GetNextWaitingForSlotAsync(
                        businessId, serviceId, date, startTime, employeeId, cancellationToken);
                    if (entry is null)
                        return;

                    // Re-check the slot actually has room now before notifying. The freed
                    // appointment may not have opened a seat (employee MaxConcurrentAppointments
                    // > 1), and an "any-employee" entry (EmployeeId null) only wants a slot that
                    // is genuinely free across the business. Skip if it is still full (0) or no
                    // longer schedulable (null) so we do not raise a false "hay hueco" alert.
                    var capacity = await _availabilityService.GetSlotCapacityAsync(
                        entry.BusinessId, entry.Date, entry.StartTime, entry.ServiceId, entry.EmployeeId, cancellationToken);
                    if (capacity is null or <= 0)
                        return;

                    // Enlist the availability event and mark the entry Notified in the
                    // SAME save (transactional outbox): the event and the Notified mark
                    // commit together, so the slot is never both "notified" with no event
                    // nor announced twice. The consumer resolves the client's contact from
                    // ClientUserId and delivers in the business language.
                    await _eventPublisher.PublishAsync(new WaitlistSlotAvailable(
                        entry.Id, entry.BusinessId, entry.EmployeeId, entry.ClientUserId,
                        entry.ServiceId, entry.Date, entry.StartTime,
                        appointment.Employee.Business.DefaultLanguage, DateTime.UtcNow),
                        cancellationToken);

                    entry.Status = WaitlistStatus.Notified;
                    _repository.Update(entry);
                    await _unitOfWork.Save(cancellationToken);
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
