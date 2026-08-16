using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    /// <summary>
    /// The cancellation rule in force for one appointment's business: the legacy single
    /// threshold plus the tiers, read in one go. When <see cref="Tiers"/> is empty the
    /// business still uses <see cref="WindowHours"/>.
    /// </summary>
    public record CancellationPolicySnapshot(int? WindowHours, IReadOnlyList<CancellationPolicyTier> Tiers)
    {
        /// <summary>No business found for the appointment: nothing to enforce.</summary>
        public static CancellationPolicySnapshot None { get; } = new(null, Array.Empty<CancellationPolicyTier>());
    }
}
