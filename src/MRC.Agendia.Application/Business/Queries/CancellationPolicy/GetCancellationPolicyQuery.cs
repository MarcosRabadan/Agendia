using MediatR;
using MRC.Agendia.Application.Business.DTO;

namespace MRC.Agendia.Application.Business.Queries.CancellationPolicy
{
    /// <summary>The cancellation tiers of a business, ordered from the most notice to the least.</summary>
    public record GetCancellationPolicyQuery(Guid BusinessId) : IRequest<IReadOnlyList<CancellationPolicyTierDto>>;
}
