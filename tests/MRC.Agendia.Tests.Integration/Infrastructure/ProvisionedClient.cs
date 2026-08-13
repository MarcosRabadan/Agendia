namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>
    /// A client identity from Harmony: the user id (the JWT "sub") plus a token
    /// carrying that same id, which is what the ownership checks compare against.
    /// Agendia no longer stores a Client row, so there is nothing to persist.
    /// </summary>
    public sealed record ProvisionedClient(string UserId, string Token);
}
