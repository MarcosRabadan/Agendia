using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Queries.Templates
{
    public record GetScheduleTemplateByIdQuery(Guid Id) : IRequest<ScheduleTemplateDto?>;
}
