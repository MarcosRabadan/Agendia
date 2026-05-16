using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public class UpdateAppointmentCommandHandler : IRequestHandler<UpdateAppointmentCommand, AppointmentDto>
    {
        private readonly IAppointmentService _service;
        private readonly IResourceAuthorizationService _auth;

        public UpdateAppointmentCommandHandler(IAppointmentService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<AppointmentDto> Handle(UpdateAppointmentCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageAppointmentAsync(request.Dto.Id);
            return await _service.UpdateAsync(request.Dto);
        }
    }
}
