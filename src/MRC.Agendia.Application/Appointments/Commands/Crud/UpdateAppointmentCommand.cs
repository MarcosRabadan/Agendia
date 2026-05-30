using MediatR;
using MRC.Agendia.Application.Appointments.DTO;

namespace MRC.Agendia.Application.Appointments.Commands.Crud
{
    public record UpdateAppointmentCommand(UpdateAppointmentDto Dto) : IRequest<AppointmentDto>;
}
