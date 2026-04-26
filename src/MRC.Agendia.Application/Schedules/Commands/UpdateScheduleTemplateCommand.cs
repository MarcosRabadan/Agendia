using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public record UpdateScheduleTemplateCommand(UpdateScheduleTemplateDto Dto) : IRequest<ScheduleTemplateDto>;
}
