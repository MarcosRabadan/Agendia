using MediatR;

namespace MRC.Agendia.Application.Schedules.Commands.Templates
{
    public record DeleteScheduleTemplateCommand(int Id) : IRequest<bool>;
}
