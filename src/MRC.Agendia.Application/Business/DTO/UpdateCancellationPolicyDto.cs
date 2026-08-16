namespace MRC.Agendia.Application.Business.DTO
{
    /// <summary>
    /// The business's whole cancellation policy. It is replaced as a unit: an empty list
    /// removes the tiers and the business falls back to its single
    /// <c>CancellationWindowHours</c> threshold.
    /// </summary>
    public record UpdateCancellationPolicyDto(IReadOnlyList<CancellationPolicyTierDto> Tiers);
}
