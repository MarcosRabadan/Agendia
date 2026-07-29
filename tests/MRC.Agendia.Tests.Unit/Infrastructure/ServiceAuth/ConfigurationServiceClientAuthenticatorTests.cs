using Microsoft.Extensions.Options;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Infrastructure.ServiceAuth;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Infrastructure.ServiceAuth
{
    /// <summary>
    /// Unit tests for the configuration-backed M2M authenticator: a valid client
    /// authenticates with its role; unknown, disabled and wrong-secret callers all
    /// collapse to a single null result.
    /// </summary>
    public class ConfigurationServiceClientAuthenticatorTests
    {
        private const string ClientId = "soundmate";
        private const string Secret = "the-real-secret-value-123456";

        private static ConfigurationServiceClientAuthenticator BuildAuthenticator(params ServiceClientOptions[] clients)
        {
            var options = new ServiceClientRegistryOptions { ServiceClients = clients.ToList() };
            var monitor = Substitute.For<IOptionsMonitor<ServiceClientRegistryOptions>>();
            monitor.CurrentValue.Returns(options);
            return new ConfigurationServiceClientAuthenticator(monitor);
        }

        private static ServiceClientOptions Client(bool enabled = true, string role = Roles.Admin) => new()
        {
            ClientId = ClientId,
            ClientSecretHash = ServiceClientSecretHasher.Hash(Secret),
            Role = role,
            Enabled = enabled
        };

        [Fact]
        public void Valid_credentials_return_the_client_with_its_role()
        {
            var result = BuildAuthenticator(Client(role: Roles.Admin)).Authenticate(ClientId, Secret);

            Assert.NotNull(result);
            Assert.Equal(ClientId, result!.ClientId);
            Assert.Equal(Roles.Admin, result.Role);
        }

        [Fact]
        public void Empty_role_falls_back_to_Admin()
        {
            var result = BuildAuthenticator(Client(role: "")).Authenticate(ClientId, Secret);

            Assert.NotNull(result);
            Assert.Equal(Roles.Admin, result!.Role);
        }

        [Fact]
        public void Wrong_secret_returns_null()
        {
            var result = BuildAuthenticator(Client()).Authenticate(ClientId, "wrong-secret");

            Assert.Null(result);
        }

        [Fact]
        public void Disabled_client_returns_null()
        {
            var result = BuildAuthenticator(Client(enabled: false)).Authenticate(ClientId, Secret);

            Assert.Null(result);
        }

        [Fact]
        public void Unknown_client_returns_null()
        {
            var result = BuildAuthenticator(Client()).Authenticate("someone-else", Secret);

            Assert.Null(result);
        }

        [Theory]
        [InlineData("", "secret")]
        [InlineData("soundmate", "")]
        public void Blank_input_returns_null(string clientId, string secret)
        {
            var result = BuildAuthenticator(Client()).Authenticate(clientId, secret);

            Assert.Null(result);
        }
    }
}
