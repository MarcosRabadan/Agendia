using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, AppointmentDto>
    {
        private readonly IAppointmentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateAppointmentCommandHandler(IAppointmentRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<AppointmentDto> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
        {
            var entity = new Appointment
            {
                ClientId = request.Dto.ClientId,
                EmployeeId = request.Dto.EmployeeId,
                ServiceId = request.Dto.ServiceId,
                StartDate = request.Dto.StartDate,
                EndDate = request.Dto.EndDate,
                Status = AppointmentStatus.Pending,
                Notes = request.Dto.Notes
            };

            await _repository.AddAsync(entity);
            await _unitOfWork.Save();

            return new AppointmentDto(entity.Id, entity.ClientId, entity.EmployeeId, entity.ServiceId, entity.StartDate, entity.EndDate, entity.Status, entity.Notes);
        }
    }
}
