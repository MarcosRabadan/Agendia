namespace MRC.Agendia.Domain.Enums
{
    // Persisted as int. Values are pinned so reordering or inserting members
    // later never silently remaps existing rows.
    public enum AppointmentStatus
    {
        Pending = 0,
        Confirmed = 1,
        Cancelled = 2,
        Completed = 3,
        NoShow = 4
    }
}
