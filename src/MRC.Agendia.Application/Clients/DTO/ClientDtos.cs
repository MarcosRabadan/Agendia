namespace MRC.Agendia.Application.Clients.DTO
{
    public record ClientDto(int Id, string Name, string Phone, string? Email);
    public record CreateClientDto(string Name, string Phone, string? Email);
    public record UpdateClientDto(int Id, string Name, string Phone, string? Email);
}
