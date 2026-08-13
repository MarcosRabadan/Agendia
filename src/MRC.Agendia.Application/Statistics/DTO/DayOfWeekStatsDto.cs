namespace MRC.Agendia.Application.Statistics.DTO
{
    /// <summary>Bookings grouped by day of week.</summary>
    public record DayOfWeekStatsDto(DayOfWeek DayOfWeek, int Count);
}
