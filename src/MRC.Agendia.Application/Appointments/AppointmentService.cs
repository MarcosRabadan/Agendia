using AutoMapper;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Notifications;
using MRC.Agendia.Application.Waitlist;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Appointments
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IClientRepository _clientRepository;
        private readonly IAppointmentSchedulingValidator _schedulingValidator;
        private readonly IBookingConcurrencyGuard _bookingGuard;
        private readonly INotificationService _notificationService;
        private readonly IWaitlistService _waitlistService;
        private readonly IAuditLogger _auditLogger;
        private readonly ICurrentUserContext _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AppointmentService(
            IAppointmentRepository repository,
            IClientRepository clientRepository,
            IAppointmentSchedulingValidator schedulingValidator,
            IBookingConcurrencyGuard bookingGuard,
            INotificationService notificationService,
            IWaitlistService waitlistService,
            IAuditLogger auditLogger,
            ICurrentUserContext currentUser,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _clientRepository = clientRepository;
            _schedulingValidator = schedulingValidator;
            _bookingGuard = bookingGuard;
            _notificationService = notificationService;
            _waitlistService = waitlistService;
            _auditLogger = auditLogger;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        #region CRUD
        public async Task<PagedResult<AppointmentDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize, cancellationToken);
            var dtos = _mapper.Map<List<AppointmentDto>>(items);
            return PagedResult<AppointmentDto>.Create(dtos, totalCount, page, pageSize);
        }

        public async Task<PagedResult<AppointmentDto>> GetPagedByClientUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            // Resolve the Client entity from the authenticated user. If the user has
            // the Client role but no Client row (e.g. row removed while the JWT is still
            // valid), return an empty page instead of leaking existence information.
            var client = await _clientRepository.GetByUserIdAsync(userId, cancellationToken);
            if (client is null)
            {
                return PagedResult<AppointmentDto>.Create(Array.Empty<AppointmentDto>(), 0, page, pageSize);
            }

            var (items, totalCount) = await _repository.GetPagedByClientIdAsync(client.Id, page, pageSize, cancellationToken);
            var dtos = _mapper.Map<List<AppointmentDto>>(items);
            return PagedResult<AppointmentDto>.Create(dtos, totalCount, page, pageSize);
        }

        public async Task<AppointmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            return entity is null ? null : _mapper.Map<AppointmentDto>(entity);
        }

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
                        clientId: dto.ClientId,
                        employeeId: dto.EmployeeId,
                        serviceId: dto.ServiceId,
                        startDate: dto.StartDate,
                        endDate: dto.EndDate,
                        cancellationToken: cancellationToken);

                    var created = _mapper.Map<Appointment>(dto);
                    await _repository.AddAsync(created, cancellationToken);
                    await _unitOfWork.Save(cancellationToken);
                    return created;
                },
                cancellationToken);

            // Best-effort confirmation email, outside the lock (never breaks the booking).
            await _notificationService.SendAppointmentConfirmationAsync(entity.Id, cancellationToken);

            return _mapper.Map<AppointmentDto>(entity);
        }

        public async Task<AppointmentDto> UpdateAsync(UpdateAppointmentDto dto, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(dto.Id, cancellationToken)
                ?? throw new AppointmentNotFoundException(dto.Id);

            var previousStatus = entity.Status;
            var previousStartDate = entity.StartDate;

            // A Client may only cancel their own appointment; Confirmed/Completed/
            // NoShow are staff-only transitions, so a Client cannot falsify the
            // status history. Notes/reschedule (status unchanged) stay allowed.
            if (dto.Status != previousStatus && dto.Status != AppointmentStatus.Cancelled && !IsStaff())
                throw new UnauthorizedAccessException(
                    "Un cliente solo puede cancelar su cita; el resto de cambios de estado son del personal.");

            // Only re-validate scheduling when a booking field actually changes.
            // A pure status/notes change (e.g. marking a past appointment Completed
            // or NoShow) must not be rejected for being in the past, and must keep
            // working even if a participant was soft-deleted afterwards (the
            // appointment keeps its history). The handler already authorizes both
            // the existing appointment and the destination, so this opens no hole.
            var bookingChanged =
                dto.StartDate != entity.StartDate ||
                dto.EndDate != entity.EndDate ||
                dto.ClientId != entity.ClientId ||
                dto.EmployeeId != entity.EmployeeId ||
                dto.ServiceId != entity.ServiceId;

            async Task ApplyAsync()
            {
                _mapper.Map(dto, entity);

                // Rescheduling to a different time re-arms the 24h reminder so it
                // is sent again for the new date.
                if (entity.StartDate != previousStartDate)
                    entity.ReminderSentAt = null;

                _repository.Update(entity);
                await _unitOfWork.Save(cancellationToken);
            }

            if (bookingChanged)
            {
                // Re-validate + persist inside the per-employee/day lock (same as
                // Create) so a reschedule cannot over-book the destination slot.
                await _bookingGuard.ExecuteSerializedAsync(
                    dto.EmployeeId,
                    DateOnly.FromDateTime(dto.StartDate),
                    async () =>
                    {
                        await _schedulingValidator.EnsureValidAsync(
                            appointmentId: dto.Id,
                            clientId: dto.ClientId,
                            employeeId: dto.EmployeeId,
                            serviceId: dto.ServiceId,
                            startDate: dto.StartDate,
                            endDate: dto.EndDate,
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

            // Best-effort cancellation email when the appointment is cancelled.
            if (previousStatus != AppointmentStatus.Cancelled && entity.Status == AppointmentStatus.Cancelled)
                await _notificationService.SendAppointmentCancellationAsync(entity.Id, cancellationToken);

            // Cancelling an occupying appointment frees a slot: notify the waitlist (best-effort).
            if (previousStatus.OccupiesCapacity() && entity.Status == AppointmentStatus.Cancelled)
                await _waitlistService.NotifyForFreedAppointmentAsync(entity.Id, cancellationToken);

            return _mapper.Map<AppointmentDto>(entity);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new AppointmentNotFoundException(id);

            // Deleting an occupying appointment frees a slot for the waitlist.
            var freedSlot = entity.Status.OccupiesCapacity();

            _repository.Delete(entity);
            await _unitOfWork.Save(cancellationToken);

            // Notify the waitlist after the deletion is persisted (best-effort).
            if (freedSlot)
                await _waitlistService.NotifyForFreedAppointmentAsync(id, cancellationToken);

            return true;
        }

        public async Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdIncludingDeletedAsync(id, cancellationToken)
                ?? throw new AppointmentNotFoundException(id);

            if (!entity.IsDeleted) return true;

            entity.IsDeleted = false;
            entity.DeletedAt = null;
            _repository.Update(entity);
            await _unitOfWork.Save(cancellationToken);
            return true;
        }
        #endregion CRUD

        public async Task<IEnumerable<AppointmentDto>> GetByBusinessIdAndDateRangeAsync(int businessId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var entities = await _repository.GetByBusinessIdAndDateRangeAsync(businessId, startDate, endDate, cancellationToken);
            return _mapper.Map<IEnumerable<AppointmentDto>>(entities);
        }

        private bool IsStaff()
            => _currentUser.IsInRole(Roles.Admin)
               || _currentUser.IsInRole(Roles.BusinessOwner)
               || _currentUser.IsInRole(Roles.Employee);
    }
}
