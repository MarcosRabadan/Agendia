using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Queries.Overrides
{
    public record GetScheduleOverridesByBusinessIdQuery(int BusinessId, DateOnly? From, DateOnly? To) : IRequest<IEnumerable<ScheduleOverrideDto>>;
}
