namespace MRC.Agendia.Application.Statistics.DTO
{
    /// <summary>Utilization of one day of the week across the whole range.</summary>
    public record WeekdayUtilizationDto(DayOfWeek Weekday, int OfferedMinutes, int BookedMinutes, double OccupancyRate);
}
