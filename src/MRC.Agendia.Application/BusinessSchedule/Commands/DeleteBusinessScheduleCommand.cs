using MediatR;

namespace MRC.Agendia.Application.BusinessSchedule.Commands
{
    public record DeleteBusinessScheduleCommand(int Id) : IRequest<bool>;
}
