using AutoMapper;
using MRC.Agendia.Application.Availability;
using MRC.Agendia.Application.Notifications;
using MRC.Agendia.Application.Waitlist.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Waitlist
{
    public class WaitlistService : IWaitlistService
    {
        private readonly IWaitlistRepository _repository;
        private readonly IClientRepository _clientRepository;
        private readonly IAvailabilityService _availabilityService;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public WaitlistService(
            IWaitlistRepository repository,
            IClientRepository clientRepository,
            IAvailabilityService availabilityService,
            IAppointmentRepository appointmentRepository,
            INotificationService notificationService,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _clientRepository = clientRepository;
            _availabilityService = availabilityService;
            _appointmentRepository = appointmentRepository;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<WaitlistEntryDto> JoinAsync(JoinWaitlistDto dto, string userId, CancellationToken cancellationToken = default)
        {
            var client = await _clientRepository.GetByUserIdAsync(userId, cancellationToken)
                ?? throw new UnauthorizedAccessException("Solo los clientes pueden usar la lista de espera.");

            // The slot must exist in the schedule and be full (capacity 0). If it
            // still has room, the client should just book directly.
            var capacity = await _availabilityService.GetSlotCapacityAsync(
                dto.BusinessId, dto.Date, dto.StartTime, dto.ServiceId, dto.EmployeeId, cancellationToken);
            if (capacity is null)
                throw new InvalidOperationException("La franja indicada no esta dentro del horario del negocio.");
            if (capacity > 0)
                throw new SlotHasCapacityException();

            if (await _repository.ExistsWaitingAsync(
                    client.Id, dto.BusinessId, dto.ServiceId, dto.Date, dto.StartTime, dto.EmployeeId, cancellationToken))
                throw new DuplicateWaitlistEntryException();

            var entry = new WaitlistEntry
            {
                BusinessId = dto.BusinessId,
                ServiceId = dto.ServiceId,
                ClientId = client.Id,
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

        public async Task LeaveAsync(int entryId, string userId, CancellationToken cancellationToken = default)
        {
            var entry = await _repository.GetByIdAsync(entryId, cancellationToken)
                ?? throw new WaitlistEntryNotFoundException(entryId);

            var client = await _clientRepository.GetByUserIdAsync(userId, cancellationToken);
            if (client is null || entry.ClientId != client.Id)
                throw new UnauthorizedAccessException("Solo puedes gestionar tus propias entradas de lista de espera.");

            if (entry.Status == WaitlistStatus.Cancelled)
                return; // idempotent

            entry.Status = WaitlistStatus.Cancelled;
            _repository.Update(entry);
            await _unitOfWork.Save(cancellationToken);
        }

        public async Task<IReadOnlyList<WaitlistEntryDto>> GetMineAsync(string userId, CancellationToken cancellationToken = default)
        {
            var client = await _clientRepository.GetByUserIdAsync(userId, cancellationToken);
            if (client is null)
                return Array.Empty<WaitlistEntryDto>();

            var entries = await _repository.GetActiveByClientAsync(client.Id, cancellationToken);
            return _mapper.Map<List<WaitlistEntryDto>>(entries);
        }

        public async Task NotifyForFreedAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default)
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

                var entry = await _repository.GetNextWaitingForSlotAsync(
                    appointment.Employee.BusinessId, appointment.ServiceId, date, startTime, appointment.EmployeeId, cancellationToken);
                if (entry is null)
                    return;

                entry.Status = WaitlistStatus.Notified;
                _repository.Update(entry);
                await _unitOfWork.Save(cancellationToken);

                await _notificationService.SendWaitlistAvailabilityAsync(entry.Id, cancellationToken);
            }
            catch
            {
                // Swallowed on purpose: notifying the waitlist is a non-critical
                // side effect of cancelling/deleting an appointment.
            }
        }
    }
}
