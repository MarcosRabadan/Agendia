using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Business
{
    /// <summary>
    /// End-to-end coverage for the per-business notification language (es/en/fr):
    /// the validator rejects unknown codes, and a valid code chosen at owner
    /// registration is persisted on the Business so notifications pick it up.
    /// </summary>
    public class BusinessLanguageIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string ValidPassword = "Owner1234!";
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public BusinessLanguageIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task RegisterOwner_IdiomaNoSoportado_400()
        {
            var dto = BuildDto(NewEmail(), NewBusinessEmail(), "xx");

            var response = await _client.PostAsJsonAsync("/api/auth/register/owner", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("VALIDATION_ERROR", body);
        }

        [Fact]
        public async Task RegisterOwner_ConIdiomaIngles_PersisteEnElNegocio()
        {
            var businessEmail = NewBusinessEmail();
            var dto = BuildDto(NewEmail(), businessEmail, "en");

            var response = await _client.PostAsJsonAsync("/api/auth/register/owner", dto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var business = await db.Businesses
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Email == businessEmail);

            Assert.NotNull(business);
            Assert.Equal("en", business!.DefaultLanguage);
        }

        [Fact]
        public async Task RegisterOwner_SinIdioma_UsaEspanolPorDefecto()
        {
            var businessEmail = NewBusinessEmail();
            // BusinessDefaultLanguage left at its default (es) by the record.
            var dto = new RegisterOwnerDto(
                Email: NewEmail(),
                Password: ValidPassword,
                FullName: "Maria Owner",
                Phone: "600111222",
                BusinessName: "Pelu Maria",
                BusinessAddress: "Calle 1, 28001 Madrid",
                BusinessPhone: "910001122",
                BusinessEmail: businessEmail,
                BusinessDescription: null);

            var response = await _client.PostAsJsonAsync("/api/auth/register/owner", dto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var business = await db.Businesses
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Email == businessEmail);

            Assert.NotNull(business);
            Assert.Equal("es", business!.DefaultLanguage);
        }

        private static string NewEmail() => $"owner-{Guid.NewGuid():N}@agendia.test";

        private static string NewBusinessEmail() => $"biz-{Guid.NewGuid():N}@agendia.test";

        private static RegisterOwnerDto BuildDto(string email, string businessEmail, string language) => new(
            Email: email,
            Password: ValidPassword,
            FullName: "Maria Owner",
            Phone: "600111222",
            BusinessName: "Pelu Maria",
            BusinessAddress: "Calle 1, 28001 Madrid",
            BusinessPhone: "910001122",
            BusinessEmail: businessEmail,
            BusinessDescription: null,
            BusinessDefaultLanguage: language);
    }
}
