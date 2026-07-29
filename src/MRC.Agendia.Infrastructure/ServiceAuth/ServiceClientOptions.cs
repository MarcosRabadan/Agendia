using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Infrastructure.ServiceAuth
{
    /// <summary>
    /// One trusted machine-to-machine client, bound from an entry of the
    /// <c>ServiceClients</c> configuration array. The secret is stored ONLY as a
    /// hash (see <see cref="ServiceClientSecretHasher"/>); the plaintext lives in
    /// user-secrets / environment variables and is never committed.
    /// </summary>
    public class ServiceClientOptions
    {
        /// <summary>Stable identifier the service authenticates with.</summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>PBKDF2 hash of the client secret (never the plaintext).</summary>
        public string ClientSecretHash { get; set; } = string.Empty;

        /// <summary>
        /// Role the issued token carries. Defaults to <see cref="Roles.Admin"/> so a
        /// service can operate across every business (v1, option A). Narrow this only
        /// once a dedicated Service role is wired into the authorization policies.
        /// </summary>
        public string Role { get; set; } = Roles.Admin;

        /// <summary>A disabled client can never obtain a token.</summary>
        public bool Enabled { get; set; } = true;
    }
}
