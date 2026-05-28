namespace MRC.Agendia.Application.Appointments.DTO
{
    /// <summary>Outcome of a series operation that only reports how many rows it affected (cancel/delete).</summary>
    public record AppointmentSeriesCountResultDto(Guid SeriesId, int Affected);
}
