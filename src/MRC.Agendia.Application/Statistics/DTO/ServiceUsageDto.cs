namespace MRC.Agendia.Application.Statistics.DTO
{
    /// <summary>Usage of a service in the range: number of bookings. The display name is
    /// resolved by the front from the catalog service using <c>ServiceId</c>.</summary>
    public record ServiceUsageDto(Guid ServiceId, int Count);
}
