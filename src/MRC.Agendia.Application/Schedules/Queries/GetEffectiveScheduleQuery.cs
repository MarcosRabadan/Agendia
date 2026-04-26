using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Queries
{
    public record GetEffectiveScheduleQuery(int BusinessId, DateOnly Date) : IRequest<EffectiveScheduleDto>;
}
