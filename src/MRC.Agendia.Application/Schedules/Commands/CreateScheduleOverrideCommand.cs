using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public record CreateScheduleOverrideCommand(CreateScheduleOverrideDto Dto) : IRequest<ScheduleOverrideDto>;
}
