using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Auth.DTO;
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
        public async Task UpdateService_OwnerB_TocaServiceDeBusinessA_DevuelveForbidden()
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

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // Sanity: the service is still in business A with the original data.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var stored = await db.Services.FindAsync(serviceOfA.Id);
            Assert.NotNull(stored);
            Assert.Equal(ownerA.Business.Id, stored!.BusinessId);
            Assert.Equal("Corte A", stored.Name);
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
