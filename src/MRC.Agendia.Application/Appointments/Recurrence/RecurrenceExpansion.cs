using MRC.Agendia.Application.Appointments.DTO;

namespace MRC.Agendia.Application.Appointments.Recurrence
{
    /// <summary>
    /// Result of expanding a recurrence pattern: the concrete candidate dates, plus the ones the
    /// pattern produced that can never be booked, already described as skips (a month without
    /// that day, a day-of-month that had already passed when the series starts, dates past the
    /// safety cap). Whether a candidate is ACTUALLY bookable - open day, capacity, time off - is
    /// decided later by the scheduling validator.
    /// </summary>
    public sealed record RecurrenceExpansion(
        IReadOnlyList<DateOnly> Dates,
        IReadOnlyList<SkippedOccurrenceDto> Skipped);
}
