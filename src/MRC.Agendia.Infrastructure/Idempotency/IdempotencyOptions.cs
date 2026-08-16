namespace MRC.Agendia.Infrastructure.Idempotency
{
    /// <summary>
    /// Options for the idempotency records, bound from the "Idempotency" config section.
    /// All values have safe defaults, so the section is optional.
    /// </summary>
    public class IdempotencyOptions
    {
        public const string SectionName = "Idempotency";

        /// <summary>
        /// How long a key is remembered. A retry after this window books again instead of
        /// replaying, so it must comfortably outlive any client retry policy. Default 24h.
        /// </summary>
        public int RetentionHours { get; set; } = 24;

        /// <summary>How often expired records are purged. Default 60 minutes.</summary>
        public int PurgeIntervalMinutes { get; set; } = 60;
    }
}
