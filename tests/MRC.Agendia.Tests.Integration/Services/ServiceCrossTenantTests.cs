using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Application.Availability.DTO;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Common;
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
        private const string OwnerPassword = "Owner1234!";

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
            var serviceOfA = await CreateServiceAsAsync(ownerA, "Corte A", price: 20m, duration: 30);

            // Owner B crafts a PUT that targets the service of A (which he does not own).
            var hijackDto = new UpdateServiceDto(
                Id: serviceOfA.Id,
                Name: "Hijacked",
                Description: null,
                DurationMinutes: 60,
                Price: 99m);

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
            Assert.Equal("Corte A", stored.Name);
        }

        [Fact]
        public async Task ListaPublicaDeNegocios_OwnerAutenticado_VeTodos_NoSoloElSuyo()
        {
            // #58 must NOT scope public catalog reads: an authenticated owner still
            // sees every business via the anonymous list (IgnoreQueryFilters), not
            // just his own. Confirms the public-read bypass works for a restricted user.
            var ownerA = await RegisterOwnerAsync("scope-list-a");
            var ownerB = await RegisterOwnerAsync("scope-list-b");

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Business?page=1&pageSize=200");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerB.Token);
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var paged = await response.Content.ReadFromJsonAsync<PagedResult<BusinessPublicDto>>();
            Assert.NotNull(paged);
            Assert.Contains(paged!.Items, b => b.Id == ownerA.Business.Id);
            Assert.Contains(paged.Items, b => b.Id == ownerB.Business.Id);
        }

        [Fact]
        public async Task UpdateService_OwnerA_EnSuPropioBusiness_AplicaCambios()
        {
            var ownerA = await RegisterOwnerAsync("svc-happy");
            var service = await CreateServiceAsAsync(ownerA, "Original", price: 10m, duration: 15);

            var dto = new UpdateServiceDto(
                Id: service.Id,
                Name: "Renombrado",
                Description: "Cambiado",
                DurationMinutes: 45,
                Price: 22m);

            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Service/{service.Id}")
            {
                Content = JsonContent.Create(dto)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerA.Token);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await response.Content.ReadFromJsonAsync<ServiceDto>();
            Assert.NotNull(updated);
            Assert.Equal("Renombrado", updated!.Name);
            Assert.Equal(45, updated.DurationMinutes);
            Assert.Equal(22m, updated.Price);
            // The service stays in its owner's business.
            Assert.Equal(ownerA.Business.Id, updated.BusinessId);
        }

        [Fact]
        public async Task GetServiceById_OwnerAutenticado_DeOtroNegocio_DevuelveOk()
        {
            // #58 regression: GET /api/Service/{id} is [AllowAnonymous] (public
            // catalog detail). Before the fix it read through the scoped GetByIdAsync,
            // so an authenticated owner browsing ANOTHER business's service got 404.
            // It must now return the service (read unscoped via GetByIdPublicAsync).
            var ownerA = await RegisterOwnerAsync("svc-detail-a");
            var ownerB = await RegisterOwnerAsync("svc-detail-b");
            var serviceOfA = await CreateServiceAsAsync(ownerA, "Corte A", price: 20m, duration: 30);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Service/{serviceOfA.Id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerB.Token);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<ServiceDto>();
            Assert.NotNull(dto);
            Assert.Equal(serviceOfA.Id, dto!.Id);
            Assert.Equal(ownerA.Business.Id, dto.BusinessId);
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
            var serviceOfA = await CreateServiceAsAsync(ownerA, "Corte A", price: 20m, duration: 30);

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

        [Fact]
        public async Task CatalogoPublicoDeServicios_OwnerAutenticado_VeServiciosDeOtroNegocio()
        {
            // #58 must NOT scope the public Service catalog: an authenticated owner
            // still sees every business's services via the anonymous list
            // (IgnoreQueryFilters), not just his own. Mirrors the public-business-list
            // test for the GET /api/Service catalog path.
            var ownerA = await RegisterOwnerAsync("svc-cat-a");
            var ownerB = await RegisterOwnerAsync("svc-cat-b");
            var serviceOfA = await CreateServiceAsAsync(ownerA, "Corte A", price: 20m, duration: 30);
            var serviceOfB = await CreateServiceAsAsync(ownerB, "Corte B", price: 25m, duration: 45);

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Service?page=1&pageSize=200");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerB.Token);
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var paged = await response.Content.ReadFromJsonAsync<PagedResult<ServiceDto>>();
            Assert.NotNull(paged);
            Assert.Contains(paged!.Items, s => s.Id == serviceOfA.Id);
            Assert.Contains(paged.Items, s => s.Id == serviceOfB.Id);
        }

        // ----- Helpers -----

        private async Task<ServiceDto> CreateServiceAsAsync(
            RegisteredOwner owner,
            string name,
            decimal price,
            int duration)
        {
            var dto = new CreateServiceDto(
                BusinessId: owner.Business.Id,
                Name: name,
                Description: null,
                DurationMinutes: duration,
                Price: price);

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

        private async Task<RegisteredOwner> RegisterOwnerAsync(string slug)
        {
            var unique = Guid.NewGuid().ToString("N");
            var email = $"{slug}-{unique}@test.local";
            var businessName = $"{slug}-{unique}";

            var registration = new RegisterOwnerDto(
                Email: email,
                Password: OwnerPassword,
                FullName: $"Owner {slug}",
                Phone: "600000000",
                BusinessName: businessName,
                BusinessAddress: "Calle Test 1",
                BusinessPhone: "910000000",
                BusinessEmail: $"info-{unique}@test.local",
                BusinessDescription: null);

            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register/owner", registration);
            registerResponse.EnsureSuccessStatusCode();
            var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(auth);

            var businessesResponse = await _client.GetAsync("/api/Business?page=1&pageSize=200");
            businessesResponse.EnsureSuccessStatusCode();
            var paged = await businessesResponse.Content.ReadFromJsonAsync<PagedResult<BusinessPublicDto>>();
            Assert.NotNull(paged);
            var business = paged!.Items.First(b => b.Name == businessName);

            return new RegisteredOwner(auth!.AccessToken, business);
        }

        private sealed record RegisteredOwner(string Token, BusinessPublicDto Business);
    }
}
