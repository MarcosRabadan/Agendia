using MediatR;
using MRC.Agendia.Application.Business.DTO;

namespace MRC.Agendia.Application.Business.Commands.CancellationPolicy
{
    /// <summary>Replaces the whole cancellation policy of a business.</summary>
    public record UpdateCancellationPolicyCommand(Guid BusinessId, UpdateCancellationPolicyDto Dto)
        : IRequest<IReadOnlyList<CancellationPolicyTierDto>>;
}
