namespace MRC.Agendia.Application.Idempotency
{
    /// <summary>What happened when a request tried to claim an idempotency key.</summary>
    public enum IdempotencyClaimOutcome
    {
        /// <summary>The key is new: this request owns it and must run.</summary>
        Claimed,

        /// <summary>The same request already completed: replay its stored response.</summary>
        Replay,

        /// <summary>An identical request holds the key and has not finished yet.</summary>
        InProgress,

        /// <summary>The key was already used for a DIFFERENT request payload.</summary>
        KeyReused
    }
}
