namespace MRC.Agendia.Application.ServiceAuth
{
    /// <summary>
    /// Validates the credentials of a machine-to-machine caller against the
    /// registry of trusted service clients.
    /// </summary>
    public interface IServiceClientAuthenticator
    {
        /// <summary>
        /// Returns the authenticated client when <paramref name="clientId"/> exists,
        /// is enabled and <paramref name="clientSecret"/> matches its stored hash
        /// (constant-time comparison); otherwise <c>null</c>. A single null result
        /// for every failure keeps the caller from leaking which clientIds exist.
        /// </summary>
        /// <param name="clientId">The service client identifier.</param>
        /// <param name="clientSecret">The plaintext secret presented by the caller.</param>
        /// <returns>The authenticated client, or <c>null</c> when authentication fails.</returns>
        AuthenticatedServiceClient? Authenticate(string clientId, string clientSecret);
    }
}
