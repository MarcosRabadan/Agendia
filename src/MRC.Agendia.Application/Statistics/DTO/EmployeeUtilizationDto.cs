namespace MRC.Agendia.Application.Statistics.DTO
{
    /// <summary>Utilization of one employee across the whole range.</summary>
    public record EmployeeUtilizationDto(Guid EmployeeId, int OfferedMinutes, int BookedMinutes, double OccupancyRate);
}
