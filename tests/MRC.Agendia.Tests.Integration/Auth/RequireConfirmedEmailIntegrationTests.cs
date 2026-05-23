using System.Net;
using System.Net.Http.Json;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Auth
{
    /// <summary>
    /// With <c>Auth:RequireConfirmedEmail = true</c>, login is blocked until the
    /// user confirms their email. After confirming, login succeeds.
    /// </summary>
    public class RequireConfirmedEmailIntegrationTests
        : IClassFixture<RequireConfirmedEmailWebApplicationFactory>
    {
        private const string ValidPassword = "Test1234!";
        private readonly RequireConfirmedEmailWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public RequireConfirmedEmailIntegrationTests(RequireConfirmedEmailWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Login_BloqueadoHastaConfirmar_LuegoFunciona()
        {
            var email = $"test-{Guid.NewGuid():N}@agendia.test";

            var register = await _client.PostAsJsonAsync("/api/auth/register/client",
                new RegisterClientDto(email, ValidPassword, "Pending User", "600000000"));
            register.EnsureSuccessStatusCode();

            // With confirmation required, registration must NOT grant a session.
            var registerBody = await register.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(registerBody);
            Assert.True(string.IsNullOrEmpty(registerBody!.AccessToken));
            Assert.True(string.IsNullOrEmpty(registerBody.RefreshToken));

            // Login is blocked while the email is unconfirmed.
            var blocked = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginDto(email, ValidPassword));
            Assert.Equal(HttpStatusCode.Unauthorized, blocked.StatusCode);
            Assert.Contains("confirmar tu email", await blocked.Content.ReadAsStringAsync());

            // Confirm using the token from the registration email.
            var confirmEmail = _factory.EmailSender.LastTo(email);
            Assert.NotNull(confirmEmail);
            var confirm = await _client.PostAsJsonAsync("/api/auth/confirm-email",
                new ConfirmEmailDto(confirmEmail!.QueryValue("userId")!, confirmEmail.QueryValue("token")!));
            Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

            // Now login works.
            var ok = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginDto(email, ValidPassword));
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }
    }
}
