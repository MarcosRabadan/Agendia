using MediatR;
using MRC.Agendia.Application.Waitlist.DTO;

namespace MRC.Agendia.Application.Waitlist.Queries
{
    public record GetMyWaitlistQuery() : IRequest<IReadOnlyList<WaitlistEntryDto>>;
}
