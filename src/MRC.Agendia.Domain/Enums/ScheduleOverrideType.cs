namespace MRC.Agendia.Domain.Enums
{
    // Persisted as int. Values are pinned so reordering or inserting members
    // later never silently remaps existing rows.
    public enum ScheduleOverrideType
    {
        Closed = 0,
        NationalHoliday = 1,
        LocalHoliday = 2,
        CustomHours = 3
    }
}
