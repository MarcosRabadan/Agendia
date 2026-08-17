using AutoMapper;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Waitlist;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Events;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Appointments
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAppointmentSchedulingValidator _schedulingValidator;
        private readonly IBookingConcurrencyGuard _bookingGuard;
        private readonly IClock _clock;
        private readonly IWaitlistService _waitlistService;
        private readonly IAuditLogger _auditLogger;
        private readonly ICurrentUserContext _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AppointmentService(IAppointmentRepository repository,
                                  IEmployeeRepository employeeRepository,
                                  IAppointmentSchedulingValidator schedulingValidator,
                                  IBookingConcurrencyGuard bookingGuard,
                                  IClock clock,
                                  IWaitlistService waitlistService,
                                  IAuditLogger auditLogger,
                                  ICurrentUserContext currentUser,
                                  IUnitOfWork unitOfWork,
                                  IMapper mapper)
        {
            _repository = repository;
            _employeeRepository = employeeRepository;
            _schedulingValidator = schedulingValidator;
            _bookingGuard = bookingGuard;
            _clock = clock;
            _waitlistService = waitlistService;
            _auditLogger = auditLogger;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        #region CRUD
        /// <inheritdoc />
        public async Task<PagedResult<AppointmentDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize, cancellationToken);
            var dtos = _mapper.Map<List<AppointmentDto>>(items);
            return PagedResult<AppointmentDto>.Create(dtos, totalCount, page, pageSize);
        }

        /// <inheritdoc />
        public async Task<PagedResult<AppointmentDto>> GetPagedByClientUserIdAsync(string userId,
                                                                                   int page,
                                                                                   int pageSize,
                                                                                   CancellationToken cancellationToken = default)
        {
            // Appointments store the client's Harmony user id directly, so filter by it
            // without any Client-entity lookup (a user with no bookings gets an empty page).
            var (items, totalCount) = await _repository.GetPagedByClientUserIdAsync(userId, page, pageSize, cancellationToken);
            var dtos = _mapper.Map<List<AppointmentDto>>(items);
            return PagedResult<AppointmentDto>.Create(dtos, totalCount, page, pageSize);
        }

        /// <inheritdoc />
        public async Task<AppointmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // WithExtras so the DTO echoes the booked extra services (#170). Read-only.
            var entity = await _repository.GetByIdWithExtrasAsync(id, cancellationToken);
            return entity is null ? null : _mapper.Map<AppointmentDto>(entity);
        }

        /// <inheritdoc />
        public async Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto, CancellationToken cancellationToken = default)
        {
            // Validate + insert inside a per-employee/day lock so two concurrent
            // bookings cannot both pass the capacity check and over-book the slot.
            var entity = await _bookingGuard.ExecuteSerializedAsync(
                dto.EmployeeId,
                DateOnly.FromDateTime(dto.StartDate),
                async () =>
                {
                    await _schedulingValidator.EnsureValidAsync(
                        appointmentId: null,
                        employeeId: dto.EmployeeId,
                        serviceId: dto.ServiceId,
                        startDate: dto.StartDate,
                        endDate: dto.EndDate,
                        extraServiceIds: dto.ExtraServiceIds,
                        clientUserId: dto.ClientUserId,
                        cancellationToken: cancellationToken);

                    var created = _mapper.Map<Appointment>(dto);
                    // Initial status: staff may choose it per booking (Pending/Confirmed
                    // only); anyone else (a client self-booking) gets the business default.
                    created.Status = IsStaff() && dto.Status is { } chosen && chosen.IsValidInitialStatus()
                        ? chosen
                        : await _repository.GetBusinessDefaultStatusByEmployeeAsync(dto.EmployeeId, cancellationToken);
                    // Multiservice (#170): attach the extra services so EF inserts
                    // them with the appointment (cascade via the navigation).
                    if (dto.ExtraServiceIds is { Count: > 0 })
                    {
                        created.ExtraServices = dto.ExtraServiceIds
                            .Select(extraServiceId => new AppointmentExtraService { ServiceId = extraServiceId })
                            .ToList();
                    }
                    await _repository.AddAsync(created, cancellationToken);
                    await _unitOfWork.Save(cancellationToken);

                    // Confirmation event, written in the SAME transaction as the
                    // appointment (transactional outbox): both commit together at the
                    // end of the guarded critical section. The appointment id is only
                    // known after the first Save, hence the second one here.
                    await RaiseAppointmentEventAsync(created, ctx => new AppointmentConfirmed(
                        ctx.AppointmentId, ctx.BusinessId, ctx.EmployeeId, ctx.ClientUserId,
                        ctx.ServiceId, ctx.StartDate, ctx.EndDate, ctx.Language, DateTime.UtcNow),
                        cancellationToken);
                    await _unitOfWork.Save(cancellationToken);
                    return created;
                },
                cancellationToken);

            // If this client was holding the slot from the waitlist (#268), the hold has
            // served its purpose: close it so it stops reserving a seat on top of the
            // appointment it just became.
            await _waitlistService.ConsumeHoldAsync(
                entity.ClientUserId, entity.EmployeeId, entity.StartDate, cancellationToken);

            return _mapper.Map<AppointmentDto>(entity);
        }

        /// <inheritdoc />
        public async Task<AppointmentDto> UpdateAsync(UpdateAppointmentDto dto, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(dto.Id, cancellationToken)
                ?? throw new AppointmentNotFoundException(dto.Id);

            var previousStatus = entity.Status;
            var previousStartDate = entity.StartDate;
            var previousEndDate = entity.EndDate;
            var previousClientUserId = entity.ClientUserId;

            // Terminal states (Completed/NoShow/Cancelled) are final: block any change
            // out of them - e.g. a client cancelling an already-Completed appointment,
            // or staff reverting a Cancelled one. A change that leaves the status
            // unchanged (notes, reschedule) is unaffected.
            if (dto.Status != previousStatus && previousStatus.IsTerminal())
                throw new InvalidAppointmentStatusTransitionException(previousStatus, dto.Status);

            // A Client may only cancel their own appointment; Confirmed/Completed/
            // NoShow are staff-only transitions, so a Client cannot falsify the
            // status history. Notes/reschedule (status unchanged) stay allowed.
            if (dto.Status != previousStatus && dto.Status != AppointmentStatus.Cancelled && !IsStaff())
                throw new UnauthorizedAccessException(
                    "A client can only cancel their appointment; the rest of the status changes are staff-only.");

            // Only re-validate scheduling when a booking field actually changes.
            // A pure status/notes change (e.g. marking a past appointment Completed
            // or NoShow) must not be rejected for being in the past, and must keep
            // working even if a participant was soft-deleted afterwards (the
            // appointment keeps its history). The handler already authorizes both
            // the existing appointment and the destination, so this opens no hole.
            var bookingChanged =
                dto.StartDate != entity.StartDate ||
                dto.EndDate != entity.EndDate ||
                dto.ClientUserId != entity.ClientUserId ||
                dto.EmployeeId != entity.EmployeeId ||
                dto.ServiceId != entity.ServiceId;

            // #171: self-service advance-notice window. Staff bypass it; a client
            // cannot cancel or reschedule an appointment that is already within the
            // business's cancellation window. Checked against the appointment's
            // CURRENT start time (you cannot act on an imminent appointment yourself).
            var becomesCancelled =
                dto.Status == AppointmentStatus.Cancelled && previousStatus != AppointmentStatus.Cancelled;
            CancellationPolicyTier? appliedTier = null;
            if (!IsStaff() && (becomesCancelled || bookingChanged))
            {
                var policy = await _repository.GetCancellationPolicyAsync(entity.Id, cancellationToken);
                appliedTier = AppointmentCancellationPolicy.EnsureSelfServiceAllowed(
                    previousStartDate, policy, _clock.BusinessNow);
            }

            async Task ApplyAsync()
            {
                _mapper.Map(dto, entity);

                // Rescheduling to a different time re-arms the 24h reminder so it
                // is sent again for the new date.
                if (entity.StartDate != previousStartDate)
                    entity.ReminderSentAt = null;

                // What the update means for the people involved, in order of precedence. All of
                // these are written in the SAME save as the change (transactional outbox).
                var timeChanged = entity.StartDate != previousStartDate || entity.EndDate != previousEndDate;
                var clientChanged = entity.ClientUserId != previousClientUserId;

                if (becomesCancelled)
                {
                    // A cancelled appointment always yields its event.
                    await RaiseAppointmentEventAsync(entity, ctx => new AppointmentCancelled(
                        ctx.AppointmentId, ctx.BusinessId, ctx.EmployeeId, ctx.ClientUserId,
                        ctx.ServiceId, ctx.StartDate, ctx.EndDate, ctx.Language, DateTime.UtcNow),
                        cancellationToken);
                }
                else if (clientChanged)
                {
                    // Handing the appointment to somebody else is TWO facts about two different
                    // people, not a reschedule (#296): the one who loses it has to hear it is
                    // gone, and the one who gets it has to be confirmed - with the final time,
                    // so a move bundled into the same request needs no extra event. The
                    // cancellation names the PREVIOUS client: the entity already holds the new
                    // one, so it is the only field not taken from it.
                    await RaiseAppointmentEventAsync(entity, ctx => new AppointmentCancelled(
                        ctx.AppointmentId, ctx.BusinessId, ctx.EmployeeId, previousClientUserId,
                        ctx.ServiceId, ctx.StartDate, ctx.EndDate, ctx.Language, DateTime.UtcNow),
                        cancellationToken);

                    await RaiseAppointmentEventAsync(entity, ctx => new AppointmentConfirmed(
                        ctx.AppointmentId, ctx.BusinessId, ctx.EmployeeId, ctx.ClientUserId,
                        ctx.ServiceId, ctx.StartDate, ctx.EndDate, ctx.Language, DateTime.UtcNow),
                        cancellationToken);
                }
                else if (timeChanged)
                {
                    // A plain move: the consumer tells the client it changed (previous -> new).
                    await RaiseAppointmentEventAsync(entity, ctx => new AppointmentRescheduled(
                        ctx.AppointmentId, ctx.BusinessId, ctx.EmployeeId, ctx.ClientUserId, ctx.ServiceId,
                        previousStartDate, previousEndDate, entity.StartDate, entity.EndDate, ctx.Language, DateTime.UtcNow),
                        cancellationToken);
                }

                _repository.Update(entity);
                await _unitOfWork.Save(cancellationToken);
            }

            if (bookingChanged)
            {
                // Multiservice (#170): re-validate the total duration against the
                // appointment's existing extra services (extras are not editable on
                // update in v1, but a reschedule must still honour their duration).
                var existingExtraServiceIds = await _repository.GetExtraServiceIdsAsync(dto.Id, cancellationToken);

                // Re-validate + persist inside the per-employee/day lock (same as
                // Create) so a reschedule cannot over-book the destination slot.
                await _bookingGuard.ExecuteSerializedAsync(
                    dto.EmployeeId,
                    DateOnly.FromDateTime(dto.StartDate),
                    async () =>
                    {
                        await _schedulingValidator.EnsureValidAsync(
                            appointmentId: dto.Id,
                            employeeId: dto.EmployeeId,
                            serviceId: dto.ServiceId,
                            startDate: dto.StartDate,
                            endDate: dto.EndDate,
                            extraServiceIds: existingExtraServiceIds,
                            clientUserId: dto.ClientUserId,
                            cancellationToken: cancellationToken);
                        await ApplyAsync();
                    },
                    cancellationToken);
            }
            else
            {
                await ApplyAsync();
            }

            if (previousStatus != entity.Status)
            {
                await _auditLogger.LogAsync(
                    AuditActions.AppointmentStatusChanged,
                    "Appointment",
                    entity.Id.ToString(),
                    new { from = previousStatus.ToString(), to = entity.Status.ToString() },
                    cancellationToken);
            }

            // (The cancellation event is published inside ApplyAsync so it commits
            // with the status change; nothing to do here.)

            // Cancelling an occupying appointment frees a slot: notify the waitlist (best-effort).
            if (previousStatus.OccupiesCapacity() && entity.Status == AppointmentStatus.Cancelled)
                await _waitlistService.NotifyForFreedAppointmentAsync(entity.Id, cancellationToken);

            // Return WITH the extra services (#170): the tracked entity was loaded
            // without them, so re-read so the response matches GET/Create.
            var refreshed = await _repository.GetByIdWithExtrasAsync(entity.Id, cancellationToken);
            var result = _mapper.Map<AppointmentDto>(refreshed ?? entity);

            // Tell the client what their own cancellation OR reschedule cost, when the business
            // states its policy as tiers (#270). Agendia does not charge it: it reports the rule.
            // The tier is only resolved for a client acting on their own appointment, so a
            // non-null value here always belongs to them (#296: it used to be computed for a
            // reschedule and then dropped, so moving a class under a 50% tier answered as if it
            // were free).
            return appliedTier is null
                ? result
                : result with
                {
                    AppliedCancellationTier = new AppliedCancellationTierDto(
                        appliedTier.MinHoursBefore, appliedTier.PenaltyKind, appliedTier.PenaltyValue)
                };
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new AppointmentNotFoundException(id);

            // #171: deleting is a hard cancel, so the self-service window applies too.
            if (!IsStaff())
            {
                var policy = await _repository.GetCancellationPolicyAsync(entity.Id, cancellationToken);
                AppointmentCancellationPolicy.EnsureSelfServiceAllowed(entity.StartDate, policy, _clock.BusinessNow);
            }

            // Deleting an occupying appointment frees a slot for the waitlist.
            var freedSlot = entity.Status.OccupiesCapacity();

            // Deleting a live appointment IS a cancellation for whoever was booked, so it
            // announces one just like PUT with status Cancelled does (#296). Before this the
            // same act notified or not depending on which verb the front happened to use.
            // Raised before the save so the event commits in the same transaction as the
            // (soft) delete; only when it still occupied a slot, mirroring the PUT path, which
            // stays silent for an appointment that was already cancelled or finished.
            if (freedSlot)
            {
                await RaiseAppointmentEventAsync(entity, ctx => new AppointmentCancelled(
                    ctx.AppointmentId, ctx.BusinessId, ctx.EmployeeId, ctx.ClientUserId,
                    ctx.ServiceId, ctx.StartDate, ctx.EndDate, ctx.Language, DateTime.UtcNow),
                    cancellationToken);
            }

            _repository.Delete(entity);
            await _unitOfWork.Save(cancellationToken);

            // Notify the waitlist after the deletion is persisted (best-effort).
            if (freedSlot)
                await _waitlistService.NotifyForFreedAppointmentAsync(id, cancellationToken);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdIncludingDeletedAsync(id, cancellationToken)
                ?? throw new AppointmentNotFoundException(id);

            if (!entity.IsDeleted) return true;

            // A past appointment, or one in a terminal status, cannot overbook anything: it is
            // history coming back, so it returns exactly as it was. A FUTURE one that still
            // occupies a slot has to fit, because the slot may well have been booked by someone
            // else while this appointment was deleted (#294).
            if (!entity.Status.OccupiesCapacity() || entity.StartDate < _clock.BusinessNow)
            {
                await ApplyRestoreAsync(entity, cancellationToken);
                return true;
            }

            // Same critical section as booking: checking capacity and writing the restore have to
            // be serialized against concurrent bookings for that employee and day, or two
            // requests could each find the last seat free.
            await _bookingGuard.ExecuteSerializedAsync(
                entity.EmployeeId,
                DateOnly.FromDateTime(entity.StartDate),
                async () =>
                {
                    await EnsureSlotStillFitsAsync(entity, cancellationToken);
                    await ApplyRestoreAsync(entity, cancellationToken);
                },
                cancellationToken);

            return true;
        }

        private async Task ApplyRestoreAsync(Appointment entity, CancellationToken cancellationToken)
        {
            entity.IsDeleted = false;
            entity.DeletedAt = null;
            _repository.Update(entity);
            await _unitOfWork.Save(cancellationToken);
        }

        // The employee must still have room for this appointment. It is counted excluding
        // itself, and a soft-deleted employee needs no check at all: nobody can book them, so
        // nothing can have taken the slot in the meantime.
        private async Task EnsureSlotStillFitsAsync(Appointment entity, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.GetByIdAsync(entity.EmployeeId, cancellationToken);
            if (employee is null)
                return;

            var overlapping = await _repository.CountOverlappingForEmployeeAsync(
                entity.EmployeeId, entity.StartDate, entity.EndDate, entity.Id, cancellationToken);

            if (overlapping >= employee.MaxConcurrentAppointments)
            {
                throw new AppointmentConflictException(
                    "The appointment cannot be restored: the slot was taken while it was deleted "
                    + "and the employee would exceed their capacity.");
            }
        }
        #endregion CRUD

        /// <inheritdoc />
        public async Task<IEnumerable<AppointmentDto>> GetByBusinessIdAndDateRangeAsync(Guid businessId,
                                                                                        DateTime startDate,
                                                                                        DateTime endDate,
                                                                                        CancellationToken cancellationToken = default)
        {
            var entities = await _repository.GetByBusinessIdAndDateRangeAsync(businessId, startDate, endDate, cancellationToken);
            return _mapper.Map<IEnumerable<AppointmentDto>>(entities);
        }

        private bool IsStaff()
            => _currentUser.IsInRole(Roles.Admin)
               || _currentUser.IsInRole(Roles.BusinessOwner)
               || _currentUser.IsInRole(Roles.Employee);

        // Raises the built event ON THE TRACKED ENTITY. The DbContext's SaveChanges override
        // enlists the entity's domain events into the outbox in the same save as the state
        // change, so nothing is published here.
        //
        // Everything the event says about the appointment is taken from the ENTITY, which
        // already holds the new state; only the owning business and its language are read, keyed
        // by the appointment's CURRENT employee. Reading the whole context by appointment id
        // instead would describe the state BEFORE the update, because the row on disk is still
        // the old one at this point - EF does not flush pending changes before running a query.
        // That is how a move to another employee ended up announcing the employee the
        // appointment had just been moved away from (#293).
        private async Task RaiseAppointmentEventAsync(Appointment appointment,
                                                      Func<AppointmentNotificationContext, IIntegrationEvent> build,
                                                      CancellationToken cancellationToken)
        {
            var business = await _repository.GetNotificationBusinessByEmployeeAsync(appointment.EmployeeId, cancellationToken);
            if (business is null)
                return;

            appointment.RaiseEvent(build(new AppointmentNotificationContext(
                appointment.Id, business.BusinessId, appointment.EmployeeId, appointment.ClientUserId,
                appointment.ServiceId, appointment.StartDate, appointment.EndDate, business.Language)));
        }
    }
}
