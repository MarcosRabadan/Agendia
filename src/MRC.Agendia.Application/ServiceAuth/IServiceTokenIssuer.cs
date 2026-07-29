namespace MRC.Agendia.Application.ServiceAuth
{
    /// <summary>Issues a signed service (client-credentials) JWT.</summary>
    public interface IServiceTokenIssuer
    {
        /// <summary>
        /// Signs a JWT for the given service client with the same key, issuer and
        /// audience the user tokens are validated with, so the existing validation
        /// (<c>AuthenticationSetup</c>) accepts it unchanged.
        /// </summary>
        /// <param name="client">The authenticated service client to mint a token for.</param>
        /// <returns>The signed token and its UTC expiry.</returns>
        ServiceToken Issue(AuthenticatedServiceClient client);
    }
}
