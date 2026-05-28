namespace MRC.Agendia.Application.Appointments.DTO
{
    /// <summary>Outcome of creating a recurring series: what was booked and what was skipped.</summary>
    public record AppointmentSeriesResultDto(
        Guid SeriesId,
        IReadOnlyList<AppointmentDto> Created,
        IReadOnlyList<SkippedOccurrenceDto> Skipped);
}
