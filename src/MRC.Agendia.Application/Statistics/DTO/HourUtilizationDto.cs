namespace MRC.Agendia.Application.Statistics.DTO
{
    /// <summary>
    /// Utilization of one hour of the day across the whole range, in minutes of agenda
    /// (offered = open minutes x employee capacity).
    /// </summary>
    public record HourUtilizationDto(int Hour, int OfferedMinutes, int BookedMinutes, double OccupancyRate);
}
