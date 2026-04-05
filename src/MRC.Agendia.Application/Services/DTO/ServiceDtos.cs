namespace MRC.Agendia.Application.Services.DTO
{
    public record ServiceDto(int Id, int BusinessId, string Name, string? Description, int DurationMinutes, decimal Price);
    public record CreateServiceDto(int BusinessId, string Name, string? Description, int DurationMinutes, decimal Price);
    public record UpdateServiceDto(int Id, int BusinessId, string Name, string? Description, int DurationMinutes, decimal Price);
}
