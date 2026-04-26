using MediatR;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public record DeleteScheduleOverrideCommand(int Id) : IRequest<bool>;
}
