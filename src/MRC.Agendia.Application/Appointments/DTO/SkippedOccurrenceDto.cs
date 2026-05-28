namespace MRC.Agendia.Application.Appointments.DTO
{
    /// <summary>
    /// An occurrence that could not be created/moved, with a machine-readable
    /// <see cref="Code"/> and a Spanish <see cref="Reason"/> for the UI to show.
    /// </summary>
    public record SkippedOccurrenceDto(DateOnly Date, string Code, string Reason);
}
