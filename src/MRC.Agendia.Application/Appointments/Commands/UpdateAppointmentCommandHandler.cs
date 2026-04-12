using MediatR;
using MRC.Agendia.Application.Appointments.DTO;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public class UpdateAppointmentCommandHandler : IRequestHandler<UpdateAppointmentCommand, AppointmentDto>
    {
        private readonly IAppointmentService _service;

        public UpdateAppointmentCommandHandler(IAppointmentService service)
        {
            _service = service;
        }

        public async Task<AppointmentDto> Handle(UpdateAppointmentCommand request, CancellationToken cancellationToken)
        {
            return await _service.UpdateAsync(request.Dto);
        }
    }
}
