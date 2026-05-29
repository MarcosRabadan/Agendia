using MediatR;
using MRC.Agendia.Application.Waitlist.DTO;

namespace MRC.Agendia.Application.Waitlist.Commands
{
    public record JoinWaitlistCommand(JoinWaitlistDto Dto) : IRequest<WaitlistEntryDto>;
}
