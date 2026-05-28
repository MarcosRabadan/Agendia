namespace MRC.Agendia.Application.Statistics.DTO
{
    /// <summary>Bookings and completed revenue grouped by hour of day (0-23, business wall-clock).</summary>
    public record HourStatsDto(int Hour, int Count, decimal Revenue);
}
