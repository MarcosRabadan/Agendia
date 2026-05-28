namespace MRC.Agendia.Application.Appointments.Recurrence
{
    /// <summary>
    /// Result of expanding a recurrence pattern into concrete candidate dates.
    /// <see cref="ShortMonths"/> holds the first day of each month that lacked the
    /// requested day-of-month (e.g. the 31st in February) so the caller can report
    /// it as a skipped occurrence.
    /// </summary>
    public sealed record RecurrenceExpansion(
        IReadOnlyList<DateOnly> Dates,
        IReadOnlyList<DateOnly> ShortMonths);
}
