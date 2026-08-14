namespace MRC.Agendia.Infrastructure.Messaging
{
    /// <summary>
    /// A serialized integration event awaiting delivery (transactional outbox).
    /// Written in the same transaction as the operation that raised it, so it is
    /// never lost if the broker is down, and delivered later by
    /// <see cref="OutboxDispatcherService"/>. This is a persistence detail of the
    /// messaging layer, not a domain entity.
    /// </summary>
    public class OutboxMessage
    {
        public Guid Id { get; set; }

        /// <summary>Event type discriminator (the event record's type name).</summary>
        public string Type { get; set; } = null!;

        /// <summary>JSON-serialized event payload.</summary>
        public string Payload { get; set; } = null!;

        /// <summary>UTC instant the event occurred.</summary>
        public DateTime OccurredOnUtc { get; set; }

        /// <summary>UTC instant it was delivered, or null while pending delivery.</summary>
        public DateTime? ProcessedOnUtc { get; set; }

        /// <summary>How many delivery attempts have been made (for diagnostics/retry).</summary>
        public int Attempts { get; set; }

        /// <summary>Last delivery error, or null. Set when an attempt fails.</summary>
        public string? Error { get; set; }
    }
}
