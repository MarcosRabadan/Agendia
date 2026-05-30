using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands.Templates
{
    public record CreateScheduleTemplateCommand(CreateScheduleTemplateDto Dto) : IRequest<ScheduleTemplateDto>;
}
