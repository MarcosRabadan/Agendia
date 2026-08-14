using MediatR;

namespace MRC.Agendia.Application.Appointments.Commands.Crud
{
    public record RestoreAppointmentCommand(Guid Id) : IRequest<bool>;
}
