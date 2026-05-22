namespace MRC.Agendia.Application.Services.DTO
{
    public record UpdateServiceDto(
        int Id,
        int BusinessId,
        string Name,
        string? Description,
        int DurationMinutes,
        decimal Price);
}
