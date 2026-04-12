using MediatR;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public class DeleteAppointmentCommandHandler : IRequestHandler<DeleteAppointmentCommand, bool>
    {
        private readonly IAppointmentService _service;

        public DeleteAppointmentCommandHandler(IAppointmentService service)
        {
            _service = service;
        }

        public async Task<bool> Handle(DeleteAppointmentCommand request, CancellationToken cancellationToken)
        {
            return await _service.DeleteAsync(request.Id);
        }
    }
}
