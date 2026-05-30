using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands.Overrides
{
    public record UpdateScheduleOverrideCommand(UpdateScheduleOverrideDto Dto) : IRequest<ScheduleOverrideDto>;
}
