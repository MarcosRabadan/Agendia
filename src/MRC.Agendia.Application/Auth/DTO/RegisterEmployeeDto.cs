namespace MRC.Agendia.Application.Auth.DTO
{
    public record RegisterEmployeeDto(
        int BusinessId,
        string Email,
        string Password,
        string FullName,
        string? Phone);
}
