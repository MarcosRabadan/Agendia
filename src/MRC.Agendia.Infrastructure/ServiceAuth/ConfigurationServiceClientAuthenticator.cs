using Microsoft.Extensions.Options;
using MRC.Agendia.Application.ServiceAuth;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Infrastructure.ServiceAuth
{
    /// <summary>
    /// Validates M2M credentials against the configured <c>ServiceClients</c> registry.
    /// An unknown clientId, a disabled client and a wrong secret all return
    /// <c>null</c> (uniform failure). Reads the options snapshot per call so a config
    /// reload takes effect without a restart.
    /// </summary>
    public class ConfigurationServiceClientAuthenticator : IServiceClientAuthenticator
    {
        // Fixed valid hash used to burn the same PBKDF2 work when there is no matching client,
        // so a missing/disabled clientId cannot be told apart from a wrong secret by response
        // time (prevents clientId enumeration by timing). Computed once per process.
        private static readonly string DummyHash = ServiceClientSecretHasher.Hash("constant-time-dummy");

        private readonly IOptionsMonitor<ServiceClientRegistryOptions> _options;

        public ConfigurationServiceClientAuthenticator(IOptionsMonitor<ServiceClientRegistryOptions> options)
        {
            _options = options;
        }

        /// <inheritdoc />
        public AuthenticatedServiceClient? Authenticate(string clientId, string clientSecret)
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                return null;

            var match = _options.CurrentValue.ServiceClients
                .FirstOrDefault(c => c.Enabled && string.Equals(c.ClientId, clientId, StringComparison.Ordinal));

            if (match is null)
            {
                // Constant-time: run the same PBKDF2 work as a real verification so a
                // missing/disabled clientId takes as long as a wrong secret, closing the
                // timing side channel that would otherwise leak which clientIds exist.
                ServiceClientSecretHasher.Verify(clientSecret, DummyHash);
                return null;
            }

            if (!ServiceClientSecretHasher.Verify(clientSecret, match.ClientSecretHash))
                return null;

            var role = string.IsNullOrWhiteSpace(match.Role) ? Roles.Admin : match.Role;
            return new AuthenticatedServiceClient(match.ClientId, role);
        }
    }
}
