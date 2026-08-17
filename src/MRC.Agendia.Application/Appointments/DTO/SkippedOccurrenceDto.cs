namespace MRC.Agendia.Application.Appointments.DTO
{
    /// <summary>
    /// An occurrence that could not be created/moved, with a machine-readable
    /// <see cref="Code"/> to branch on and a human-readable <see cref="Reason"/> for the UI to
    /// show. Covers both what the calendar rules out (a month without that day, the safety cap)
    /// and what the agenda rules out for that date (closed day, full slot, time off).
    /// </summary>
    public record SkippedOccurrenceDto(DateOnly Date, string Code, string Reason);
}
