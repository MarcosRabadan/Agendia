using MediatR;

namespace MRC.Agendia.Application.Appointments.Commands.Crud
{
    public record DeleteAppointmentCommand(int Id) : IRequest<bool>;
}
