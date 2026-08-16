namespace MRC.Agendia.Domain.Constants
{
    /// <summary>Size limits of the <c>Idempotency-Key</c> handling (#266).</summary>
    public static class IdempotencyLimits
    {
        /// <summary>
        /// Longest accepted header value. A UUID is 36 characters; the cap is there to
        /// keep a caller from turning the header into an unbounded write.
        /// </summary>
        public const int MaxHeaderKeyLength = 128;

        /// <summary>
        /// Longest stored key. The storage key prefixes the header value with the caller's
        /// user id, so two callers can never collide on the same key.
        /// </summary>
        public const int MaxStorageKeyLength = 320;
    }
}
