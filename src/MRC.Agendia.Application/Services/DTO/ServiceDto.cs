namespace MRC.Agendia.Application.Services.DTO
{
    // Scheduling projection returned by the provisioning POST/PUT. Carries no catalog
    // data (name/description/price): that lives in the management/catalog service.
    public record ServiceDto(
        Guid Id,
        Guid BusinessId,
        int DurationMinutes);
}
