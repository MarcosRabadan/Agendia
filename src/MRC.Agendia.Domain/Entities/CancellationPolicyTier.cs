using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Domain.Entities
{
    /// <summary>
    /// One step of a business's cancellation policy: "cancelling at least
    /// <see cref="MinHoursBefore"/> hours ahead has THIS consequence". The tiers of a
    /// business are read together and the one that applies is the most demanding whose
    /// threshold the client still meets.
    ///
    /// Tiers are optional: a business with none keeps the single-threshold behaviour of
    /// <c>Business.CancellationWindowHours</c>.
    /// </summary>
    public class CancellationPolicyTier
    {
        public Guid Id { get; set; }

        public Guid BusinessId { get; set; }

        /// <summary>
        /// Minimum advance notice, in hours, for this tier to apply. The set always
        /// includes a 0 tier, so every possible moment falls in exactly one tier.
        /// </summary>
        public int MinHoursBefore { get; set; }

        /// <summary>What cancelling within this tier costs the client.</summary>
        public CancellationPenaltyKind PenaltyKind { get; set; }

        /// <summary>
        /// Percentage (0-100) or amount, depending on <see cref="PenaltyKind"/>.
        /// Null for <see cref="CancellationPenaltyKind.None"/> and
        /// <see cref="CancellationPenaltyKind.NotAllowed"/>.
        /// </summary>
        public decimal? PenaltyValue { get; set; }
    }
}
