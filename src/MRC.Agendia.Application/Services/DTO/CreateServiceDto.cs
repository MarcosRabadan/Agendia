namespace MRC.Agendia.Application.Services.DTO
{
    public record CreateServiceDto(
        int BusinessId,
        string Name,
        string? Description,
        int DurationMinutes,
        decimal Price);
}
