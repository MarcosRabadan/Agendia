using MediatR;

namespace MRC.Agendia.Application.Waitlist.Commands.Leave
{
    public record LeaveWaitlistCommand(int EntryId) : IRequest<bool>;
}
