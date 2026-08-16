namespace MRC.Agendia.Application.Idempotency
{
    /// <summary>
    /// Result of claiming an idempotency key. The response fields are only filled for
    /// <see cref="IdempotencyClaimOutcome.Replay"/>, where they carry the stored answer
    /// of the original request.
    /// </summary>
    public record IdempotencyClaim(
        IdempotencyClaimOutcome Outcome,
        int? StatusCode = null,
        string? ResponseBody = null);
}
