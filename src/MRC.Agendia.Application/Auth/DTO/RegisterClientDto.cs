namespace MRC.Agendia.Application.Auth.DTO
{
    public record RegisterClientDto(
        string Email,
        string Password,
        string FullName,
        string Phone);
}
