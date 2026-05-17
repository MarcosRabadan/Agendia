using System.Net;
using System.Net.Http.Json;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Auth
{
    /// <summary>
    /// Integration tests for the public Owner self-registration flow (issue #81).
    /// `POST /api/auth/register/owner` is now anonymous and creates the
    /// ApplicationUser, the BusinessOwner role assignment, the Business itself
    /// and the auto-Employee for the owner (logic from #71).
    /// </summary>
    public class OwnerRegistrationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string ValidPassword = "Owner1234!";
        private readonly HttpClient _client;

        public OwnerRegistrationIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task RegisterOwner_Anonimo_DevuelveTokensYRolBusinessOwner()
        {
            var email = NewEmail();
            var dto = new RegisterOwnerDto(
                Email: email,
                Password: ValidPassword,
                FullName: "Maria Owner",
                Phone: "600111222",
                BusinessName: "Pelu Maria",
                BusinessAddress: "Calle 1, 28001 Madrid",
                BusinessPhone: "910001122",
                BusinessEmail: "info@pelumaria.test",
                BusinessDescription: "Peluqueria de barrio");

            // No Authorization header at all - this used to require Admin role.
            var response = await _client.PostAsJsonAsync("/api/auth/register/owner", dto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(body);
            Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
            Assert.Equal(email, body.User.Email);
            Assert.Contains("BusinessOwner", body.User.Roles);
        }

        [Fact]
        public async Task RegisterOwner_EmailDuplicado_400()
        {
            var email = NewEmail();
            var dto = BuildValidDto(email);

            var first = await _client.PostAsJsonAsync("/api/auth/register/owner", dto);
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            // A second registration with the same email must fail. AuthService
            // surfaces it as InvalidOperationException -> 400 via the middleware.
            var second = await _client.PostAsJsonAsync("/api/auth/register/owner", dto);

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
            var body = await second.Content.ReadAsStringAsync();
            Assert.Contains("Ya existe un usuario con ese email.", body);
        }

        [Fact]
        public async Task RegisterOwner_PayloadInvalido_400()
        {
            // Empty password / business email triggers FluentValidation -> 400
            // with the "VALIDATION_ERROR" envelope.
            var dto = new RegisterOwnerDto(
                Email: NewEmail(),
                Password: string.Empty,
                FullName: "Maria Owner",
                Phone: "600111222",
                BusinessName: "Pelu Maria",
                BusinessAddress: "Calle 1, 28001 Madrid",
                BusinessPhone: "910001122",
                BusinessEmail: string.Empty,
                BusinessDescription: null);

            var response = await _client.PostAsJsonAsync("/api/auth/register/owner", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("VALIDATION_ERROR", body);
        }

        private static string NewEmail() => $"owner-{Guid.NewGuid():N}@agendia.test";

        private static RegisterOwnerDto BuildValidDto(string email) => new(
            Email: email,
            Password: ValidPassword,
            FullName: "Maria Owner",
            Phone: "600111222",
            BusinessName: "Pelu Maria",
            BusinessAddress: "Calle 1, 28001 Madrid",
            BusinessPhone: "910001122",
            BusinessEmail: "info@pelumaria.test",
            BusinessDescription: null);
    }
}
