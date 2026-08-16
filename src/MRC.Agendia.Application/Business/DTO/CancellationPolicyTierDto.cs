using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Business.DTO
{
    /// <summary>
    /// One step of the business's cancellation policy: cancelling at least
    /// <paramref name="MinHoursBefore" /> hours ahead costs the client
    /// <paramref name="PenaltyKind" /> (<paramref name="PenaltyValue" /> is the percentage
    /// or the amount, and is null when there is nothing to charge).
    /// </summary>
    public record CancellationPolicyTierDto(
        int MinHoursBefore,
        CancellationPenaltyKind PenaltyKind,
        decimal? PenaltyValue = null);
}
