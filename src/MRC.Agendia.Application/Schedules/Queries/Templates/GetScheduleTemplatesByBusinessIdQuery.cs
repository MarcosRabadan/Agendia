using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Queries.Templates
{
    public record GetScheduleTemplatesByBusinessIdQuery(Guid BusinessId) : IRequest<IEnumerable<ScheduleTemplateDto>>;
}
