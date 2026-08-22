namespace MRC.Agendia.Application.Waitlist
{
    /// <summary>
    /// Options for the waitlist priority hold (#268), bound from the "Waitlist" config
    /// section. All values have safe defaults, so the section is optional.
    ///
    /// <para>Injected as a plain instance rather than <c>IOptions&lt;T&gt;</c> so the
    /// Application layer keeps needing no configuration packages; the binding happens in
    /// Infrastructure.</para>
    /// </summary>
    public class WaitlistOptions
    {
        public const string SectionName = "Waitlist";

        /// <summary>
        /// How long the notified client keeps the slot to themselves. Long enough to read
        /// the notification and book, short enough not to freeze the agenda. Default 15 min.
        /// </summary>
        public int HoldMinutes { get; set; } = 15;

        /// <summary>How often expired holds are swept and the queue moved on. Default 1 min.</summary>
        public int ExpiryIntervalMinutes { get; set; } = 1;

        /// <summary>Maximum expired holds processed per sweep. Default 50.</summary>
        public int ExpiryBatchSize { get; set; } = 50;

        /// <summary>
        /// How many queued clients a freed slot may be checked against before giving up (#350).
        /// Candidates are walked FIFO and each one costs a capacity read, all inside the booking
        /// lock, so this caps the critical section. The common case - the first candidate fits -
        /// costs the same as one. Default 10.
        /// </summary>
        public int NotifyCandidateLimit { get; set; } = 10;
    }
}
