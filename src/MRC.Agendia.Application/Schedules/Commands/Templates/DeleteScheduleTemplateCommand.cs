using MediatR;

namespace MRC.Agendia.Application.Schedules.Commands.Templates
{
    public record DeleteScheduleTemplateCommand(Guid Id) : IRequest<bool>;
}
