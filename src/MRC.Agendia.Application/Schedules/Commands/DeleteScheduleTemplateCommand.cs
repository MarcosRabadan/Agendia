using MediatR;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public record DeleteScheduleTemplateCommand(int Id) : IRequest<bool>;
}
