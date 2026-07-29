using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Infrastructure.ServiceAuth;

namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>
    /// Test host with a configured registry of trusted M2M clients (#232), on top of
    /// the standard <see cref="CustomWebApplicationFactory"/> (InMemory DB, forged
    /// Harmony signing key). One enabled client and one disabled client are seeded;
    /// their secret hashes are computed at construction from the known plaintext, so
    /// nothing hard-codes a hash.
    /// </summary>
    public class ServiceAuthWebApplicationFactory : CustomWebApplicationFactory
    {
        public const string ClientId = "soundmate";
        public const string ClientSecret = "integration-service-secret-value-1234567890";
        public const string DisabledClientId = "disabled-service";

        private static readonly string SecretHash = ServiceClientSecretHasher.Hash(ClientSecret);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ServiceAuth:TokenLifetimeMinutes"] = "15",

                    ["ServiceClients:0:ClientId"] = ClientId,
                    ["ServiceClients:0:ClientSecretHash"] = SecretHash,
                    ["ServiceClients:0:Role"] = Roles.Admin,
                    ["ServiceClients:0:Enabled"] = "true",

                    ["ServiceClients:1:ClientId"] = DisabledClientId,
                    ["ServiceClients:1:ClientSecretHash"] = SecretHash,
                    ["ServiceClients:1:Role"] = Roles.Admin,
                    ["ServiceClients:1:Enabled"] = "false"
                });
            });

            base.ConfigureWebHost(builder);
        }
    }
}
