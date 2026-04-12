namespace MRC.Agendia.Application.Clients.DTO
{
    public record ClientDto(
        int Id, 
        string Name,
        string Phone,
        string? Email);
}
