using MediatR;

namespace MRC.Agendia.Application.Waitlist.Commands.Leave
{
    public record LeaveWaitlistCommand(Guid EntryId) : IRequest<bool>;
}
