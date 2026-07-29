namespace MRC.Agendia.Application.ServiceAuth.DTO
{
    /// <summary>
    /// Service token returned to a trusted service. There is NO refresh token:
    /// the service requests a fresh token with its secret when this one expires.
    /// <paramref name="ExpiresAt"/> is a UTC instant.
    /// </summary>
    public record ServiceTokenResponseDto(string AccessToken, DateTime ExpiresAt, string TokenType);
}
