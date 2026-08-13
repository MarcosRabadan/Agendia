using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.SoftDelete
{
    /// <summary>
    /// End-to-end coverage for issue #52: deleting a resource hides it (soft delete)
    /// instead of removing the row, audit fields are filled by the interceptor, and
    /// an Admin can restore a previously deleted resource.
    /// </summary>
    public class SoftDeleteIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public SoftDeleteIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task DeleteService_OcultaElServicio_PeroNoBorraLaFila()
        {
            var owner = await RegisterOwnerAsync("sd-del");
            var service = await CreateServiceAsAsync(owner);

            using (var del = new HttpRequestMessage(HttpMethod.Delete, $"/api/Service/{service.Id}"))
            {
                del.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
                var delResponse = await _client.SendAsync(del);
                Assert.Equal(HttpStatusCode.NoContent, delResponse.StatusCode);
            }

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

            // Hidden by the global query filter, but still physically present.
            Assert.False(await db.Services.AnyAsync(s => s.Id == service.Id));
            var stored = await db.Services.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == service.Id);
            Assert.NotNull(stored);
            Assert.True(stored!.IsDeleted);
            Assert.NotNull(stored.DeletedAt);
        }

        [Fact]
        public async Task RestoreService_ComoAdmin_RecuperaElServicio()
        {
            var owner = await RegisterOwnerAsync("sd-restore");
            var service = await CreateServiceAsAsync(owner);

            using (var del = new HttpRequestMessage(HttpMethod.Delete, $"/api/Service/{service.Id}"))
            {
                del.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
                (await _client.SendAsync(del)).EnsureSuccessStatusCode();
            }

            var adminToken = NewAdminToken();
            using (var restore = new HttpRequestMessage(HttpMethod.Post, $"/api/Service/{service.Id}/restore"))
            {
                restore.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
                var restoreResponse = await _client.SendAsync(restore);
                Assert.Equal(HttpStatusCode.NoContent, restoreResponse.StatusCode);
            }

            // Live again after restore: the global query filter no longer hides it.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var stored = await db.Services.FirstOrDefaultAsync(s => s.Id == service.Id);
            Assert.NotNull(stored);
            Assert.False(stored!.IsDeleted);
        }

        [Fact]
        public async Task RestoreService_ComoOwner_DevuelveForbidden()
        {
            var owner = await RegisterOwnerAsync("sd-forbidden");
            var service = await CreateServiceAsAsync(owner);

            using var restore = new HttpRequestMessage(HttpMethod.Post, $"/api/Service/{service.Id}/restore");
            restore.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
            var response = await _client.SendAsync(restore);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Availability_DeNegocioBorrado_Devuelve404()
        {
            var owner = await RegisterOwnerAsync("sd-avail");
            var service = await CreateServiceAsAsync(owner);

            var adminToken = NewAdminToken();
            using (var del = new HttpRequestMessage(HttpMethod.Delete, $"/api/Business/{owner.Business.Id}"))
            {
                del.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
                (await _client.SendAsync(del)).EnsureSuccessStatusCode();
            }

            var date = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");
            var get = await _client.GetAsync(
                $"/api/businesses/{owner.Business.Id}/availability?date={date}&serviceId={service.Id}");

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Fact]
        public async Task CreateService_RellenaAuditFields()
        {
            var owner = await RegisterOwnerAsync("sd-audit");
            var service = await CreateServiceAsAsync(owner);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var stored = await db.Services.FindAsync(service.Id);

            Assert.NotNull(stored);
            Assert.NotEqual(default, stored!.CreatedAt);
            Assert.False(string.IsNullOrEmpty(stored.CreatedBy));
            Assert.Null(stored.UpdatedAt);
        }

        // ----- Helpers -----

        private async Task<ServiceDto> CreateServiceAsAsync(ProvisionedOwner owner)
        {
            var dto = new CreateServiceDto(owner.Business.Id, DurationMinutes: 30);

            return await TestProvisioning.PostAsync<CreateServiceDto, ServiceDto>(
                _client, "/api/Service", dto, owner.Token);
        }

        private Task<ProvisionedOwner> RegisterOwnerAsync(string slug) =>
            TestProvisioning.ProvisionOwnerAsync(_client, slug);

        // Agendia no longer stores users, so an Admin is just a forged Harmony token.
        private static string NewAdminToken() =>
            TestTokenFactory.Create($"harmony-admin-{Guid.NewGuid():N}", Roles.Admin);
    }
}
