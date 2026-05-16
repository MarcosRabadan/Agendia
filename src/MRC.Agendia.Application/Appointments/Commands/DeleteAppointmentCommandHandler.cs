using MediatR;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public class DeleteAppointmentCommandHandler : IRequestHandler<DeleteAppointmentCommand, bool>
    {
        private readonly IAppointmentService _service;
        private readonly IResourceAuthorizationService _auth;

        public DeleteAppointmentCommandHandler(IAppointmentService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<bool> Handle(DeleteAppointmentCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageAppointmentAsync(request.Id);
            return await _service.DeleteAsync(request.Id);
        }
    }
}
