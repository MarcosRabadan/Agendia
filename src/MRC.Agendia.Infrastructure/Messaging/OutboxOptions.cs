namespace MRC.Agendia.Infrastructure.Messaging
{
    /// <summary>
    /// Options for the transactional-outbox dispatcher, bound from the "Outbox" config
    /// section. All values have safe defaults, so the section is optional.
    /// </summary>
    public class OutboxOptions
    {
        public const string SectionName = "Outbox";

        /// <summary>How often the dispatcher polls for pending events. Default 10s.</summary>
        public int PollIntervalSeconds { get; set; } = 10;

        /// <summary>Maximum events delivered per poll. Default 20.</summary>
        public int BatchSize { get; set; } = 20;

        /// <summary>
        /// Delivery attempts after which a message is dead-lettered (excluded from the
        /// active poll so it cannot block newer messages). Default 10.
        /// </summary>
        public int MaxAttempts { get; set; } = 10;

        /// <summary>Processed messages older than this are purged. Default 7 days.</summary>
        public int RetentionDays { get; set; } = 7;

        /// <summary>How often the purge runs (not every poll). Default 60 minutes.</summary>
        public int PurgeIntervalMinutes { get; set; } = 60;
    }
}
