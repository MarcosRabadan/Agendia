namespace MRC.Agendia.Application.Clients.DTO
{
    public record CreateClientDto(
        string Name,
        string Phone,
        string? Email);
}
