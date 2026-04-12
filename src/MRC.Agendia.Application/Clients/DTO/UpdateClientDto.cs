namespace MRC.Agendia.Application.Clients.DTO
{
    public record UpdateClientDto(
        int Id,
        string Name, 
        string Phone, 
        string? Email);
}
