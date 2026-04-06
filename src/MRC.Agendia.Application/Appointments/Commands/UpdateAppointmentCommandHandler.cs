using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public class UpdateAppointmentCommandHandler : IRequestHandler<UpdateAppointmentCommand, AppointmentDto>
    {
        private readonly IAppointmentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateAppointmentCommandHandler(IAppointmentRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<AppointmentDto> Handle(UpdateAppointmentCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Dto.Id)
                ?? throw new KeyNotFoundException($"Appointment with Id {request.Dto.Id} not found.");

            entity.ClientId = request.Dto.ClientId;
            entity.EmployeeId = request.Dto.EmployeeId;
            entity.ServiceId = request.Dto.ServiceId;
            entity.StartDate = request.Dto.StartDate;
            entity.EndDate = request.Dto.EndDate;
            entity.Status = request.Dto.Status;
            entity.Notes = request.Dto.Notes;

            _repository.Update(entity);
            await _unitOfWork.Save();

            return new AppointmentDto(entity.Id, entity.ClientId, entity.EmployeeId, entity.ServiceId, entity.StartDate, entity.EndDate, entity.Status, entity.Notes);
        }
    }
}
