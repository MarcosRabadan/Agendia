using AutoMapper;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Appointments
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IAppointmentSchedulingValidator _schedulingValidator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AppointmentService(
            IAppointmentRepository repository,
            IAppointmentSchedulingValidator schedulingValidator,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _schedulingValidator = schedulingValidator;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        #region CRUD
        public async Task<IEnumerable<AppointmentDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<AppointmentDto>>(entities);
        }

        public async Task<AppointmentDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity is null ? null : _mapper.Map<AppointmentDto>(entity);
        }

        public async Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto)
        {
            // Validate the appointment against the business schedule and
            // existing appointments BEFORE persisting it.
            await _schedulingValidator.EnsureValidAsync(
                appointmentId: null,
                clientId: dto.ClientId,
                employeeId: dto.EmployeeId,
                serviceId: dto.ServiceId,
                startDate: dto.StartDate,
                endDate: dto.EndDate);

            var entity = _mapper.Map<Appointment>(dto);
            await _repository.AddAsync(entity);
            await _unitOfWork.Save();
            return _mapper.Map<AppointmentDto>(entity);
        }

        public async Task<AppointmentDto> UpdateAsync(UpdateAppointmentDto dto)
        {
            var entity = await _repository.GetByIdAsync(dto.Id)
                ?? throw new KeyNotFoundException($"Appointment with Id {dto.Id} not found.");

            // Validate the new state against the schedule and other
            // appointments, excluding the current one from the conflict check.
            await _schedulingValidator.EnsureValidAsync(
                appointmentId: dto.Id,
                clientId: dto.ClientId,
                employeeId: dto.EmployeeId,
                serviceId: dto.ServiceId,
                startDate: dto.StartDate,
                endDate: dto.EndDate);

            _mapper.Map(dto, entity);
            _repository.Update(entity);
            await _unitOfWork.Save();
            return _mapper.Map<AppointmentDto>(entity);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Appointment with Id {id} not found.");

            _repository.Delete(entity);
            await _unitOfWork.Save();
            return true;
        }
        #endregion CRUD

        public async Task<IEnumerable<AppointmentDto>> GetByBusinessIdAndDateRangeAsync(int businessId, DateTime startDate, DateTime endDate)
        {
            ValidateRangeQuery(startDate, endDate);
            var entities = await _repository.GetByBusinessIdAndDateRangeAsync(businessId, startDate, endDate);
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
