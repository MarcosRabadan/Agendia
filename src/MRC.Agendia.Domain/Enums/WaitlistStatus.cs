namespace MRC.Agendia.Domain.Enums
{
    // Persisted as int. Values are pinned so reordering never remaps stored rows.
    public enum WaitlistStatus
    {
        Waiting = 0,

        /// <summary>Told the slot is free and holding it until <c>HoldUntil</c> (#268).</summary>
        Notified = 1,

        Cancelled = 2,

        /// <summary>The hold ran out before the client booked; the queue moved on.</summary>
        Expired = 3,

        /// <summary>The client booked the slot they were holding.</summary>
        Booked = 4
    }
}
