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

            // Whether the clientId exists is not sensitive; the SECRET comparison is
            // the one that must be constant-time, and it is (FixedTimeEquals inside
            // the hasher). A missing/disabled client short-circuits to null.
            if (match is null)
                return null;

            if (!ServiceClientSecretHasher.Verify(clientSecret, match.ClientSecretHash))
                return null;

            var role = string.IsNullOrWhiteSpace(match.Role) ? Roles.Admin : match.Role;
            return new AuthenticatedServiceClient(match.ClientId, role);
        }
    }
}
