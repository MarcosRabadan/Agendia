using MediatR;

namespace MRC.Agendia.Application.Schedules.Commands.Overrides
{
    public record DeleteScheduleOverrideCommand(int Id) : IRequest<bool>;
}
