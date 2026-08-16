using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.TimeOff.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.TimeOff.Commands
{
    public class CreateEmployeeTimeOffCommandHandler
        : IRequestHandler<CreateEmployeeTimeOffCommand, CreateEmployeeTimeOffResultDto>
    {
        private readonly IEmployeeTimeOffRepository _repository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IResourceAuthorizationService _auth;
        private readonly IUnitOfWork _unitOfWork;

        public CreateEmployeeTimeOffCommandHandler(IEmployeeTimeOffRepository repository,
                                                   IEmployeeRepository employeeRepository,
                                                   IAppointmentRepository appointmentRepository,
                                                   IResourceAuthorizationService auth,
                                                   IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _employeeRepository = employeeRepository;
            _appointmentRepository = appointmentRepository;
            _auth = auth;
            _unitOfWork = unitOfWork;
        }

        public async Task<CreateEmployeeTimeOffResultDto> Handle(CreateEmployeeTimeOffCommand request,
                                                                 CancellationToken cancellationToken)
        {
            // Managing an employee's agenda is the same permission as editing them.
            await _auth.EnsureCanUpdateEmployeeAsync(request.EmployeeId, cancellationToken);

            _ = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
                ?? throw new EmployeeNotFoundException(request.EmployeeId);

            var timeOff = new EmployeeTimeOff
            {
                EmployeeId = request.EmployeeId,
                Start = request.Dto.Start,
                End = request.Dto.End,
                Reason = request.Dto.Reason
            };

            await _repository.AddAsync(timeOff, cancellationToken);
            await _unitOfWork.Save(cancellationToken);

            // Appointments already booked inside the block stay put: the block stops NEW
            // bookings, it does not cancel what the client already agreed to. They are
            // reported so the staff can move or cancel them deliberately.
            var collidingIds = await _appointmentRepository.GetOverlappingIdsForEmployeeAsync(
                request.EmployeeId, request.Dto.Start, request.Dto.End, cancellationToken);

            return new CreateEmployeeTimeOffResultDto(
                new EmployeeTimeOffDto(timeOff.Id, timeOff.EmployeeId, timeOff.Start, timeOff.End, timeOff.Reason),
                collidingIds);
        }
    }
}
