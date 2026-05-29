namespace MRC.Agendia.Domain.Enums
{
    // Persisted as int. Values are pinned so reordering never remaps stored rows.
    public enum WaitlistStatus
    {
        Waiting = 0,
        Notified = 1,
        Cancelled = 2
    }
}
