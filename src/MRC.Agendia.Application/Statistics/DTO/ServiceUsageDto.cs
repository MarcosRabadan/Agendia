namespace MRC.Agendia.Application.Statistics.DTO
{
    /// <summary>Usage of a service in the range: number of bookings and completed revenue.</summary>
    public record ServiceUsageDto(int ServiceId, string ServiceName, int Count, decimal Revenue);
}
