namespace MRC.Agendia.Application.Statistics.DTO
{
    /// <summary>Bookings grouped by hour of day (0-23, business wall-clock).</summary>
    public record HourStatsDto(int Hour, int Count);
}
