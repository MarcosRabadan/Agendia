using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Clients
{
    /// <summary>
    /// Per-business clients (#225): owner/staff can create account-less client records
    /// for their own business and list them; cross-tenant access is denied (403).
    /// </summary>
    public class BusinessClientsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string OwnerPassword = "Owner1234!";
        private readonly HttpClient _client;

        public BusinessClientsIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateBusinessClient_Owner_EnSuNegocio_DevuelveCreatedConBusinessId()
        {
            var owner = await RegisterOwnerAsync("cli-a");

            var created = await CreateBusinessClientAsync(owner, "Cliente Mostrador", "600111222");

            Assert.Equal(owner.Business.Id, created.BusinessId);
            Assert.Equal("Cliente Mostrador", created.Name);
        }

        [Fact]
        public async Task ListBusinessClients_Owner_VeSusClientes()
        {
            var owner = await RegisterOwnerAsync("cli-list");
            var created = await CreateBusinessClientAsync(owner, "Ana", "600111333");

            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/businesses/{owner.Business.Id}/clients?page=1&pageSize=200");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var paged = await response.Content.ReadFromJsonAsync<PagedResult<ClientDto>>();
            Assert.NotNull(paged);
            Assert.Contains(paged!.Items, c => c.Id == created.Id && c.BusinessId == owner.Business.Id);
        }

        [Fact]
        public async Task CreateBusinessClient_OwnerA_EnNegocioB_DevuelveForbidden()
        {
            var ownerA = await RegisterOwnerAsync("cli-x");
            var ownerB = await RegisterOwnerAsync("cli-y");

            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/businesses/{ownerB.Business.Id}/clients")
            {
                Content = JsonContent.Create(new CreateClientDto("Intruso", "600000000", null))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerA.Token);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ListBusinessClients_OwnerA_DeNegocioB_DevuelveForbidden()
        {
            var ownerA = await RegisterOwnerAsync("cli-p");
            var ownerB = await RegisterOwnerAsync("cli-q");

            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/businesses/{ownerB.Business.Id}/clients");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerA.Token);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ----- Helpers -----

        private async Task<ClientDto> CreateBusinessClientAsync(RegisteredOwner owner, string name, string phone)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/businesses/{owner.Business.Id}/clients")
            {
                Content = JsonContent.Create(new CreateClientDto(name, phone, null))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<ClientDto>();
            Assert.NotNull(created);
            return created!;
        }

        private async Task<RegisteredOwner> RegisterOwnerAsync(string slug)
        {
            var uniqueSuffix = Guid.NewGuid().ToString("N");
            var email = $"{slug}-{uniqueSuffix}@test.local";
            var businessName = $"{slug}-{uniqueSuffix}";

            var registration = new RegisterOwnerDto(
                Email: email,
                Password: OwnerPassword,
                FullName: $"Owner {slug}",
                Phone: "600000000",
                BusinessName: businessName,
                BusinessAddress: "Calle Test 1",
                BusinessPhone: "910000000",
                BusinessEmail: $"info-{uniqueSuffix}@test.local",
                BusinessDescription: null);

            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register/owner", registration);
            registerResponse.EnsureSuccessStatusCode();
            var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(auth);

            var businessesResponse = await _client.GetAsync("/api/Business?page=1&pageSize=200");
            businessesResponse.EnsureSuccessStatusCode();
            var paged = await businessesResponse.Content.ReadFromJsonAsync<PagedResult<BusinessDto>>();
            Assert.NotNull(paged);
            var business = paged!.Items.First(b => b.Name == businessName);

            return new RegisteredOwner(auth!.AccessToken, business);
        }

        private sealed record RegisteredOwner(string Token, BusinessDto Business);
    }
}
