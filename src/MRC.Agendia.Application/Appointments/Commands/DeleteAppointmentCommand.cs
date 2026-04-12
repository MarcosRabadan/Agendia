using MediatR;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public record DeleteAppointmentCommand(int Id) : IRequest<bool>;
}
