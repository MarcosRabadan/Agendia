namespace MRC.Agendia.Domain.Enums
{
    /// <summary>
    /// Consequence of cancelling inside a given tier of the business's cancellation
    /// policy. Agendia only states the rule: charging the penalty (when there is one)
    /// belongs to the payments/management service.
    /// </summary>
    public enum CancellationPenaltyKind
    {
        /// <summary>Free cancellation.</summary>
        None = 0,

        /// <summary>A percentage of the service price is charged (the price lives in the catalog).</summary>
        Percentage = 1,

        /// <summary>A fixed amount is charged.</summary>
        FixedAmount = 2,

        /// <summary>Self-service cancellation is not allowed this close to the appointment.</summary>
        NotAllowed = 3
    }
}
