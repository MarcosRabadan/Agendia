using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MRC.Agendia.Application.ServiceAuth.DTO;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Auth
{
    /// <summary>
    /// End-to-end tests for the machine-to-machine token endpoint (#232):
    /// POST /api/auth/service-token issues a JWT for valid credentials (401
    /// otherwise), and that token is accepted by the existing protected endpoints,
    /// operating across businesses without a per-business restriction.
    /// </summary>
    public class ServiceTokenEndpointTests : IClassFixture<ServiceAuthWebApplicationFactory>
    {
        private readonly ServiceAuthWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ServiceTokenEndpointTests(ServiceAuthWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private Task<HttpResponseMessage> RequestTokenAsync(string clientId, string clientSecret) =>
            _client.PostAsJsonAsync("/api/auth/service-token",
                new ServiceTokenRequestDto(clientId, clientSecret));

        [Fact]
        public async Task Valid_credentials_return_200_with_a_bearer_token()
        {
            var response = await RequestTokenAsync(
                ServiceAuthWebApplicationFactory.ClientId,
                ServiceAuthWebApplicationFactory.ClientSecret);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<ServiceTokenResponseDto>();
            Assert.NotNull(body);
            Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
            Assert.Equal("Bearer", body.TokenType);
            Assert.True(body.ExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task Wrong_secret_returns_401()
        {
            var response = await RequestTokenAsync(
                ServiceAuthWebApplicationFactory.ClientId, "wrong-secret");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Unknown_client_returns_401()
        {
            var response = await RequestTokenAsync("does-not-exist", "whatever");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Disabled_client_returns_401()
        {
            var response = await RequestTokenAsync(
                ServiceAuthWebApplicationFactory.DisabledClientId,
                ServiceAuthWebApplicationFactory.ClientSecret);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Missing_fields_return_400()
        {
            var response = await RequestTokenAsync("", "");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Service_token_is_accepted_by_protected_endpoints_across_businesses()
        {
            var serviceToken = await ObtainServiceTokenAsync();

            // Two independent businesses, each provisioned the way Harmony would.
            var businessA = await TestProvisioning.ProvisionOwnerAsync(_client, "svc-a");
            var businessB = await TestProvisioning.ProvisionOwnerAsync(_client, "svc-b");

            // The service token (no per-business scope) reads a Staff-gated,
            // business-scoped endpoint for BOTH businesses.
            var readA = await GetWithTokenAsync($"/api/businesses/{businessA.Business.Id}/clients?page=1&pageSize=10", serviceToken);
            var readB = await GetWithTokenAsync($"/api/businesses/{businessB.Business.Id}/clients?page=1&pageSize=10", serviceToken);

            Assert.Equal(HttpStatusCode.OK, readA.StatusCode);
            Assert.Equal(HttpStatusCode.OK, readB.StatusCode);
        }

        private async Task<string> ObtainServiceTokenAsync()
        {
            var response = await RequestTokenAsync(
                ServiceAuthWebApplicationFactory.ClientId,
                ServiceAuthWebApplicationFactory.ClientSecret);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<ServiceTokenResponseDto>();
            return body!.AccessToken;
        }

        private async Task<HttpResponseMessage> GetWithTokenAsync(string url, string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await _client.SendAsync(request);
        }
    }
}
