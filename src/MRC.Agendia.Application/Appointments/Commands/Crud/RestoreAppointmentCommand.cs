using MediatR;

namespace MRC.Agendia.Application.Appointments.Commands.Crud
{
    public record RestoreAppointmentCommand(int Id) : IRequest<bool>;
}
