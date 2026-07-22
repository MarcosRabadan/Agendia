using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

        private async Task<ClientDto> CreateBusinessClientAsync(ProvisionedOwner owner, string name, string phone)
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

        private Task<ProvisionedOwner> RegisterOwnerAsync(string slug) =>
            TestProvisioning.ProvisionOwnerAsync(_client, slug);
    }
}
