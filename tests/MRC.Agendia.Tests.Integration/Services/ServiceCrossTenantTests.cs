using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Availability.DTO;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Services
{
    /// <summary>
    /// Integration tests for issue #91. Cover the cross-tenant takeover that was
    /// possible on PUT /api/Service before the fix:
    ///   - UpdateServiceCommandHandler validated dto.BusinessId (the destination)
    ///     instead of the existing service, so an Owner of B could move a service
    ///     of A into B by sending a crafted DTO.
    /// After the fix:
    ///   - Auth is now resolved against the EXISTING service (EnsureCanManageServiceAsync).
    ///   - UpdateServiceDto no longer carries BusinessId, so a service cannot be
    ///     relocated to another tenant on update at all.
    /// </summary>
    public class ServiceCrossTenantTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ServiceCrossTenantTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task UpdateService_OwnerB_TocaServiceDeBusinessA_NoLoEncuentra()
        {
            var ownerA = await RegisterOwnerAsync("svc-a");
            var ownerB = await RegisterOwnerAsync("svc-b");

            // Owner A creates a service in his own business (legitimate).
            var serviceOfA = await CreateServiceAsAsync(ownerA, duration: 30);

            // Owner B crafts a PUT that targets the service of A (which he does not own).
            var hijackDto = new UpdateServiceDto(Id: serviceOfA.Id, DurationMinutes: 60);

            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Service/{serviceOfA.Id}")
            {
                Content = JsonContent.Create(hijackDto)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerB.Token);

            var response = await _client.SendAsync(request);

            // Defense in depth (#58): the global business filter hides business A's
            // service from owner B, so the handler cannot resolve it -> 404 (stronger
            // than the previous 403: it does not even leak that the service exists).
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // Sanity: the service is still in business A with the original data.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var stored = await db.Services.FindAsync(serviceOfA.Id);
            Assert.NotNull(stored);
            Assert.Equal(ownerA.Business.Id, stored!.BusinessId);
            Assert.Equal(30, stored.DurationMinutes); // unchanged: the hijack did not apply
        }

        [Fact]
        public async Task UpdateService_OwnerA_EnSuPropioBusiness_AplicaCambios()
        {
            var ownerA = await RegisterOwnerAsync("svc-happy");
            var service = await CreateServiceAsAsync(ownerA, duration: 15);

            var dto = new UpdateServiceDto(Id: service.Id, DurationMinutes: 45);

            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Service/{service.Id}")
            {
                Content = JsonContent.Create(dto)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerA.Token);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await response.Content.ReadFromJsonAsync<ServiceDto>();
            Assert.NotNull(updated);
            Assert.Equal(45, updated!.DurationMinutes);
            // The service stays in its owner's business.
            Assert.Equal(ownerA.Business.Id, updated.BusinessId);
        }

        [Fact]
        public async Task Disponibilidad_OwnerAutenticado_DeOtroNegocio_NoDevuelve404()
        {
            // #58 regression: GET /api/businesses/{id}/availability is [AllowAnonymous]
            // (public booking flow). Before the fix the business/service reads were
            // scoped, so an authenticated owner querying ANOTHER business got 404. It
            // must now resolve (200) - the day may be closed, but it is not hidden.
            var ownerA = await RegisterOwnerAsync("avail-a");
            var ownerB = await RegisterOwnerAsync("avail-b");
            var serviceOfA = await CreateServiceAsAsync(ownerA, duration: 30);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/businesses/{ownerA.Business.Id}/availability?date=2099-01-05&serviceId={serviceOfA.Id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerB.Token);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var availability = await response.Content.ReadFromJsonAsync<AvailabilityDto>();
            Assert.NotNull(availability);
            Assert.Equal(ownerA.Business.Id, availability!.BusinessId);
        }

        // ----- Helpers -----

        private async Task<ServiceDto> CreateServiceAsAsync(ProvisionedOwner owner, int duration)
        {
            var dto = new CreateServiceDto(owner.Business.Id, duration);

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Service")
            {
                Content = JsonContent.Create(dto)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<ServiceDto>();
            Assert.NotNull(created);
            return created!;
        }

        // Every call provisions a brand new owner user id, so owner A and owner B
        // are distinct identities holding distinct tokens.
        private Task<ProvisionedOwner> RegisterOwnerAsync(string slug) =>
            TestProvisioning.ProvisionOwnerAsync(_client, slug);
    }
}
