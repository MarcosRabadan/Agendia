using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Business
{
    /// <summary>
    /// Integration tests for the public Business endpoints. After this PR the
    /// anonymous GETs return a customer-facing projection that:
    ///   - omits the business email,
    ///   - hides inactive businesses (filtered at repo level),
    ///   - remains accessible without a token.
    /// </summary>
    public class BusinessPublicEndpointTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string OwnerPassword = "Owner1234!";

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public BusinessPublicEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAll_SinToken_DevuelveItemsSinEmail()
        {
            var registered = await RegisterOwnerAsync("pub-list");

            var response = await _client.GetAsync("/api/business?page=1&pageSize=200");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var paged = await response.Content.ReadFromJsonAsync<PagedResult<BusinessPublicDto>>();
            Assert.NotNull(paged);
            var ours = Assert.Single(paged!.Items, b => b.Id == registered.Business.Id);
            Assert.False(string.IsNullOrWhiteSpace(ours.Name));

            // Belt and braces: confirm the email field is not present in the raw
            // JSON either, so we are not just reading null from a strongly-typed
            // DTO that hid it.
            var rawJson = await ReadRawResponseAsync(response);
            Assert.DoesNotContain("\"email\"", rawJson, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetById_NegocioActivo_DevuelveDtoPublicoSinEmail()
        {
            var registered = await RegisterOwnerAsync("pub-byid");

            var response = await _client.GetAsync($"/api/business/{registered.Business.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<BusinessPublicDto>();
            Assert.NotNull(dto);
            Assert.Equal(registered.Business.Id, dto!.Id);

            var rawJson = await ReadRawResponseAsync(response);
            Assert.DoesNotContain("\"email\"", rawJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"isActive\"", rawJson, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetById_NegocioInactivo_DevuelveNotFound()
        {
            var registered = await RegisterOwnerAsync("pub-inactive");

            // The public API has no endpoint to deactivate a business yet, so we
            // flip the flag directly in the underlying DbContext. The factory's
            // InMemory database is shared across the class fixture.
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
                var entity = await db.Businesses.FindAsync(registered.Business.Id);
                Assert.NotNull(entity);
                entity!.IsActive = false;
                await db.SaveChangesAsync();
            }

            var response = await _client.GetAsync($"/api/business/{registered.Business.Id}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetAll_NegocioInactivo_NoApareceEnListado()
        {
            var visible = await RegisterOwnerAsync("pub-visible");
            var hidden = await RegisterOwnerAsync("pub-hidden");

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
                var entity = await db.Businesses.FindAsync(hidden.Business.Id);
                Assert.NotNull(entity);
                entity!.IsActive = false;
                await db.SaveChangesAsync();
            }

            var response = await _client.GetAsync("/api/business?page=1&pageSize=200");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var paged = await response.Content.ReadFromJsonAsync<PagedResult<BusinessPublicDto>>();
            Assert.NotNull(paged);
            Assert.Contains(paged!.Items, b => b.Id == visible.Business.Id);
            Assert.DoesNotContain(paged.Items, b => b.Id == hidden.Business.Id);
        }

        // ----- Helpers -----

        private static async Task<string> ReadRawResponseAsync(HttpResponseMessage response)
        {
            // The content is already buffered after ReadFromJsonAsync, but
            // re-read defensively via a fresh string copy.
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.GetRawText();
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
                BusinessDescription: "Negocio de pruebas");

            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register/owner", registration);
            registerResponse.EnsureSuccessStatusCode();

            // The public GET works even before login, so we use it to resolve
            // the new business by its unique name without any token plumbing.
            var listResponse = await _client.GetAsync("/api/business?page=1&pageSize=200");
            listResponse.EnsureSuccessStatusCode();
            var paged = await listResponse.Content.ReadFromJsonAsync<PagedResult<BusinessPublicDto>>();
            Assert.NotNull(paged);
            var business = paged!.Items.First(b => b.Name == businessName);

            return new RegisteredOwner(business);
        }

        private sealed record RegisteredOwner(BusinessPublicDto Business);
    }
}
