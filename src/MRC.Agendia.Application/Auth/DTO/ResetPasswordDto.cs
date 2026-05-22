namespace MRC.Agendia.Application.Auth.DTO
{
    public record ResetPasswordDto(
        string Email,
        string Token,
        string NewPassword);
}
