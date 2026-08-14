namespace MRC.Agendia.Application.Services.DTO
{
    public record CreateServiceDto(
        Guid BusinessId,
        int DurationMinutes);
}
