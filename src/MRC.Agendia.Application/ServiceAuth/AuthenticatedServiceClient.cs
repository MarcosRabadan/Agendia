namespace MRC.Agendia.Application.ServiceAuth
{
    /// <summary>
    /// A trusted service whose credentials were validated, together with the role
    /// its issued token must carry.
    /// </summary>
    public sealed record AuthenticatedServiceClient(string ClientId, string Role);
}
