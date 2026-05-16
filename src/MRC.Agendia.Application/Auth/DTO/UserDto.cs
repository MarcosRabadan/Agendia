namespace MRC.Agendia.Application.Auth.DTO
{
    public record UserDto(
        string Id,
        string Email,
        string FullName,
        string? Phone,
        bool IsActive,
        IEnumerable<string> Roles);
}
