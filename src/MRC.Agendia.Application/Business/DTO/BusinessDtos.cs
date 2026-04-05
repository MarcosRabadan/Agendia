namespace MRC.Agendia.Application.Business.DTO
{
    public record BusinessDto(int Id, string Name, string? Description, string Address, string Phone, string Email, bool IsActive);
    public record CreateBusinessDto(string Name, string? Description, string Address, string Phone, string Email);
    public record UpdateBusinessDto(int Id, string Name, string? Description, string Address, string Phone, string Email, bool IsActive);
}
