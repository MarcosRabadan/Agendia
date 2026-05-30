using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands.Templates
{
    public record UpdateScheduleTemplateCommand(UpdateScheduleTemplateDto Dto) : IRequest<ScheduleTemplateDto>;
}
