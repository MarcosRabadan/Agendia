using MediatR;

namespace MRC.Agendia.Application.Schedules.Commands.Overrides
{
    public record DeleteScheduleOverrideCommand(Guid Id) : IRequest<bool>;
}
