namespace MRC.Agendia.Application.Appointments.DTO
{
    /// <summary>Outcome of moving a series: which occurrences moved and which were skipped.</summary>
    public record MoveAppointmentSeriesResultDto(
        Guid SeriesId,
        IReadOnlyList<AppointmentDto> Moved,
        IReadOnlyList<SkippedOccurrenceDto> Skipped);
}
