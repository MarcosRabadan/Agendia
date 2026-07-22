using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Business
{
    /// <summary>
    /// End-to-end coverage for the per-business notification language (es/en/fr):
    /// the validator rejects unknown codes, and a valid code chosen when the
    /// business is created is persisted so notifications pick it up.
    ///
    /// The language used to travel on the owner-registration payload; now that
    /// Harmony owns registration it is set on CreateBusinessDto instead.
    /// </summary>
    public class BusinessLanguageIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public BusinessLanguageIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateBusiness_IdiomaNoSoportado_400()
        {
            var dto = BuildDto(NewBusinessEmail(), "xx");

            var response = await PostBusinessAsync(dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("VALIDATION_ERROR", body);
        }

        [Fact]
        public async Task CreateBusiness_ConIdiomaIngles_PersisteEnElNegocio()
        {
            var businessEmail = NewBusinessEmail();
            var dto = BuildDto(businessEmail, "en");

            var response = await PostBusinessAsync(dto);
            response.EnsureSuccessStatusCode();

            Assert.Equal("en", await GetStoredLanguageAsync(businessEmail));
        }

        [Fact]
        public async Task CreateBusiness_SinIdioma_UsaEspanolPorDefecto()
        {
            var businessEmail = NewBusinessEmail();

            // Posted as an anonymous object so DefaultLanguage is genuinely ABSENT
            // from the JSON, exercising the record's default instead of sending "es".
            var payload = new
            {
                Name = "Pelu Maria",
                Description = (string?)null,
                Address = "Calle 1, 28001 Madrid",
                Phone = "910001122",
                Email = businessEmail,
                OwnerUserId = NewOwnerUserId()
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Business")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NewAdminToken());

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            Assert.Equal("es", await GetStoredLanguageAsync(businessEmail));
        }

        // ----- Helpers -----

        private async Task<HttpResponseMessage> PostBusinessAsync(CreateBusinessDto dto)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Business")
            {
                Content = JsonContent.Create(dto)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NewAdminToken());

            return await _client.SendAsync(request);
        }

        private async Task<string?> GetStoredLanguageAsync(string businessEmail)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var business = await db.Businesses
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Email == businessEmail);

            Assert.NotNull(business);
            return business!.DefaultLanguage;
        }

        private static CreateBusinessDto BuildDto(string businessEmail, string language) => new(
            Name: "Pelu Maria",
            Description: null,
            Address: "Calle 1, 28001 Madrid",
            Phone: "910001122",
            Email: businessEmail,
            OwnerUserId: NewOwnerUserId(),
            DefaultLanguage: language);

        private static string NewBusinessEmail() => $"biz-{Guid.NewGuid():N}@agendia.test";

        private static string NewOwnerUserId() => $"harmony-owner-{Guid.NewGuid():N}";

        private static string NewAdminToken() =>
            TestTokenFactory.Create($"harmony-admin-{Guid.NewGuid():N}", Roles.Admin);
    }
}
