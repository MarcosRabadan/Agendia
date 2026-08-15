using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Queries.Effective
{
    public record GetEffectiveScheduleQuery(Guid BusinessId, DateOnly Date) : IRequest<EffectiveScheduleDto>;
}
