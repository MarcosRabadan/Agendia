namespace MRC.Agendia.Application.ServiceAuth.DTO
{
    /// <summary>Client-credentials request body for <c>POST /api/auth/service-token</c>.</summary>
    public record ServiceTokenRequestDto(string ClientId, string ClientSecret);
}
