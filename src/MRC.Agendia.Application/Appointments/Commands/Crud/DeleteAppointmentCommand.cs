using MediatR;

namespace MRC.Agendia.Application.Appointments.Commands.Crud
{
    public record DeleteAppointmentCommand(Guid Id) : IRequest<bool>;
}
