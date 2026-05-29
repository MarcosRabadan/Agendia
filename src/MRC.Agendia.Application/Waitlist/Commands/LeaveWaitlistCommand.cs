using MediatR;

namespace MRC.Agendia.Application.Waitlist.Commands
{
    public record LeaveWaitlistCommand(int EntryId) : IRequest<bool>;
}
