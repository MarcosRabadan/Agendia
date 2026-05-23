using MediatR;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public class RestoreAppointmentCommandHandler : IRequestHandler<RestoreAppointmentCommand, bool>
    {
        private readonly IAppointmentService _service;

        public RestoreAppointmentCommandHandler(IAppointmentService service)
        {
            _service = service;
        }

        public async Task<bool> Handle(RestoreAppointmentCommand request, CancellationToken cancellationToken)
        {
            return await _service.RestoreAsync(request.Id, cancellationToken);
        }
    }
}
