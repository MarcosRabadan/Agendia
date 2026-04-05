using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public record CreateAppointmentCommand(CreateAppointmentDto Dto) : IRequest<AppointmentDto>;
    public record UpdateAppointmentCommand(UpdateAppointmentDto Dto) : IRequest<AppointmentDto>;
    public record DeleteAppointmentCommand(int Id) : IRequest<bool>;

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

    public class DeleteAppointmentCommandHandler : IRequestHandler<DeleteAppointmentCommand, bool>
    {
        private readonly IAppointmentRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteAppointmentCommandHandler(IAppointmentRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteAppointmentCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id)
                ?? throw new KeyNotFoundException($"Appointment with Id {request.Id} not found.");

            _repository.Delete(entity);
            await _unitOfWork.Save();
            return true;
        }
    }
}
