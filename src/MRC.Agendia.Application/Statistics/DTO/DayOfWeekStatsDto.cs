namespace MRC.Agendia.Application.Statistics.DTO
{
    /// <summary>Bookings and completed revenue grouped by day of week.</summary>
    public record DayOfWeekStatsDto(DayOfWeek DayOfWeek, int Count, decimal Revenue);
}
