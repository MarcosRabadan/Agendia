namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>
    /// A client with a Harmony account: the persisted row's id plus a token whose
    /// "sub" matches the row's UserId (which is what the ownership checks compare).
    /// </summary>
    public sealed record ProvisionedClient(string UserId, string Token, int ClientId);
}
