using AutoMapper;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Notifications;
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
        private readonly INotificationService _notificationService;
        private readonly IAuditLogger _auditLogger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AppointmentService(
            IAppointmentRepository repository,
            IClientRepository clientRepository,
            IAppointmentSchedulingValidator schedulingValidator,
            INotificationService notificationService,
            IAuditLogger auditLogger,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _clientRepository = clientRepository;
            _schedulingValidator = schedulingValidator;
            _notificationService = notificationService;
            _auditLogger = auditLogger;
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
            // Validate the appointment against the business schedule and
            // existing appointments BEFORE persisting it.
            await _schedulingValidator.EnsureValidAsync(
                appointmentId: null,
                clientId: dto.ClientId,
                employeeId: dto.EmployeeId,
                serviceId: dto.ServiceId,
                startDate: dto.StartDate,
                endDate: dto.EndDate,
                cancellationToken: cancellationToken);

            var entity = _mapper.Map<Appointment>(dto);
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.Save(cancellationToken);

            // Best-effort confirmation email (never breaks the booking).
            await _notificationService.SendAppointmentConfirmationAsync(entity.Id, cancellationToken);

            return _mapper.Map<AppointmentDto>(entity);
        }

        public async Task<AppointmentDto> UpdateAsync(UpdateAppointmentDto dto, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(dto.Id, cancellationToken)
                ?? throw new AppointmentNotFoundException(dto.Id);

            var previousStatus = entity.Status;

            // Validate the new state against the schedule and other
            // appointments, excluding the current one from the conflict check.
            await _schedulingValidator.EnsureValidAsync(
                appointmentId: dto.Id,
                clientId: dto.ClientId,
                employeeId: dto.EmployeeId,
                serviceId: dto.ServiceId,
                startDate: dto.StartDate,
                endDate: dto.EndDate,
                cancellationToken: cancellationToken);

            _mapper.Map(dto, entity);
            _repository.Update(entity);
            await _unitOfWork.Save(cancellationToken);

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

            return _mapper.Map<AppointmentDto>(entity);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new AppointmentNotFoundException(id);

            _repository.Delete(entity);
            await _unitOfWork.Save(cancellationToken);
            return true;
        }
        #endregion CRUD

        public async Task<IEnumerable<AppointmentDto>> GetByBusinessIdAndDateRangeAsync(int businessId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            ValidateRangeQuery(startDate, endDate);
            var entities = await _repository.GetByBusinessIdAndDateRangeAsync(businessId, startDate, endDate, cancellationToken);
            return entities is null ? Enumerable.Empty<AppointmentDto>() : _mapper.Map<IEnumerable<AppointmentDto>>(entities);
        }

        /// <summary>
        /// Validates the parameters of a read query (date range lookup). This is
        /// independent of <see cref="IAppointmentSchedulingValidator"/>, which
        /// only validates appointment creation/update.
        /// </summary>
        private static void ValidateRangeQuery(DateTime startDate, DateTime endDate)
        {
            if (startDate == DateTime.MinValue || endDate == DateTime.MinValue
                || startDate == DateTime.MaxValue || endDate == DateTime.MaxValue)
            {
                throw new ArgumentException("StartDate and EndDate must be valid dates");
            }

            if (startDate > endDate)
            {
                throw new ArgumentException("StartDate must be earlier than or equal to EndDate.");
            }
        }
    }
}
