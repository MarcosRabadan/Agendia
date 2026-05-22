using System.Net;
using System.Net.Http.Json;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Auth
{
    /// <summary>
    /// Coverage of the email-confirmation flow (issue #57): registering sends a
    /// confirmation email with a token, and confirm-email accepts it.
    /// </summary>
    public class EmailConfirmationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string ValidPassword = "Test1234!";
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public EmailConfirmationIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Registro_EnviaEmailDeConfirmacion_Y_ConfirmEmailFunciona()
        {
            var email = $"test-{Guid.NewGuid():N}@agendia.test";

            var register = await _client.PostAsJsonAsync("/api/auth/register/client",
                new RegisterClientDto(email, ValidPassword, "Confirm User", "600000000"));
            register.EnsureSuccessStatusCode();

            var confirmEmail = _factory.EmailSender.LastTo(email);
            Assert.NotNull(confirmEmail);
            Assert.Contains("Confirma", confirmEmail!.Subject);
            var userId = confirmEmail.QueryValue("userId");
            var token = confirmEmail.QueryValue("token");
            Assert.False(string.IsNullOrWhiteSpace(userId));
            Assert.False(string.IsNullOrWhiteSpace(token));

            var confirm = await _client.PostAsJsonAsync("/api/auth/confirm-email",
                new ConfirmEmailDto(userId!, token!));
            Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        }

        [Fact]
        public async Task ConfirmEmail_TokenInvalido_400()
        {
            var email = $"test-{Guid.NewGuid():N}@agendia.test";
            var register = await _client.PostAsJsonAsync("/api/auth/register/client",
                new RegisterClientDto(email, ValidPassword, "Confirm User", "600000000"));
            register.EnsureSuccessStatusCode();

            var userId = _factory.EmailSender.LastTo(email)!.QueryValue("userId");

            var confirm = await _client.PostAsJsonAsync("/api/auth/confirm-email",
                new ConfirmEmailDto(userId!, "not-a-real-token"));

            Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);
        }
    }
}
