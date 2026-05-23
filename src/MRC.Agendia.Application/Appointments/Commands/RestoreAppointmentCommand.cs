using MediatR;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public record RestoreAppointmentCommand(int Id) : IRequest<bool>;
}
