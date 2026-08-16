namespace MRC.Agendia.Application.Idempotency
{
    /// <summary>
    /// Durable record of the requests already served under an <c>Idempotency-Key</c>, so a
    /// retry (double submit, network timeout) returns the original answer instead of
    /// creating a second appointment. Durable rather than in-memory on purpose: it must
    /// survive restarts and be shared by every instance.
    /// </summary>
    public interface IIdempotencyStore
    {
        /// <summary>
        /// Claims the key for this request, atomically. The key is claimed BEFORE the
        /// operation runs, so a concurrent twin finds it in flight instead of both passing
        /// an "is it there yet?" check and booking twice.
        /// </summary>
        /// <param name="key">Storage key (already scoped to the caller).</param>
        /// <param name="requestHash">Hash of the endpoint plus the request payload.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>Whether the caller owns the key, must replay, or must be rejected.</returns>
        Task<IdempotencyClaim> TryClaimAsync(string key, string requestHash, CancellationToken cancellationToken = default);

        /// <summary>Stores the successful response so later retries can replay it.</summary>
        Task CompleteAsync(string key,
                           int statusCode,
                           string responseBody,
                           CancellationToken cancellationToken = default);

        /// <summary>
        /// Drops the claim after a failed attempt, so the client can retry the same key.
        /// A rejected request is not an outcome worth replaying.
        /// </summary>
        Task ReleaseAsync(string key, CancellationToken cancellationToken = default);
    }
}
