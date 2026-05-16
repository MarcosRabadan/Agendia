namespace MRC.Agendia.Application.Auth.DTO
{
    public record AuthResponseDto(
        string AccessToken,
        DateTime AccessTokenExpiresAt,
        string RefreshToken,
        DateTime RefreshTokenExpiresAt,
        UserDto User);
}
