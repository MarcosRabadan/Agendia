using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Application.Appointments.Commands.Crud
{
    public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, AppointmentDto>
    {
        private readonly IAppointmentService _service;
        private readonly IResourceAuthorizationService _auth;

        public CreateAppointmentCommandHandler(IAppointmentService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<AppointmentDto> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanCreateAppointmentAsync(request.Dto.ClientId, request.Dto.EmployeeId, cancellationToken);
            return await _service.CreateAsync(request.Dto, cancellationToken);
        }
    }
}
