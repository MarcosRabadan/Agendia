namespace MRC.Agendia.Application.ServiceAuth
{
    /// <summary>A freshly signed service token and the UTC instant it expires.</summary>
    public sealed record ServiceToken(string AccessToken, DateTime ExpiresAtUtc);
}
