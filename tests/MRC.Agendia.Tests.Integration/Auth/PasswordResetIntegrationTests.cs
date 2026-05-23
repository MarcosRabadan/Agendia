using System.Net;
using System.Net.Http.Json;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Auth
{
    /// <summary>
    /// End-to-end coverage of the forgot/reset password flow (issue #57):
    /// request a reset, grab the token from the captured email, set a new
    /// password, and confirm login works with the new credentials.
    /// </summary>
    public class PasswordResetIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string ValidPassword = "Test1234!";
        private const string NewPassword = "Brand5678!";
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PasswordResetIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task ForgotThenReset_PermiteLoginConNuevaPassword()
        {
            var email = await RegisterClientAsync();

            var forgot = await _client.PostAsJsonAsync("/api/auth/forgot-password",
                new ForgotPasswordDto(email));
            Assert.Equal(HttpStatusCode.NoContent, forgot.StatusCode);

            var resetEmail = await _factory.EmailSender.WaitForAsync(email, e => e.Subject.Contains("Restablecer"));
            Assert.NotNull(resetEmail);
            Assert.Contains("Restablecer", resetEmail!.Subject);
            var token = resetEmail.QueryValue("token");
            Assert.False(string.IsNullOrWhiteSpace(token));

            var reset = await _client.PostAsJsonAsync("/api/auth/reset-password",
                new ResetPasswordDto(email, token!, NewPassword));
            Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

            // New password works.
            var loginNew = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginDto(email, NewPassword));
            Assert.Equal(HttpStatusCode.OK, loginNew.StatusCode);

            // Old password no longer works.
            var loginOld = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginDto(email, ValidPassword));
            Assert.Equal(HttpStatusCode.Unauthorized, loginOld.StatusCode);
        }

        [Fact]
        public async Task ForgotPassword_EmailInexistente_204_SinEnviarCorreo()
        {
            var email = $"missing-{Guid.NewGuid():N}@agendia.test";

            var forgot = await _client.PostAsJsonAsync("/api/auth/forgot-password",
                new ForgotPasswordDto(email));

            // Anti-enumeration: always 204, no email for an unknown address.
            Assert.Equal(HttpStatusCode.NoContent, forgot.StatusCode);
            Assert.False(_factory.EmailSender.AnyTo(email));
        }

        [Fact]
        public async Task ResetPassword_TokenInvalido_400()
        {
            var email = await RegisterClientAsync();

            var reset = await _client.PostAsJsonAsync("/api/auth/reset-password",
                new ResetPasswordDto(email, "not-a-real-token", NewPassword));

            Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
        }

        private async Task<string> RegisterClientAsync()
        {
            var email = $"test-{Guid.NewGuid():N}@agendia.test";
            var response = await _client.PostAsJsonAsync("/api/auth/register/client",
                new RegisterClientDto(email, ValidPassword, "Reset User", "600000000"));
            response.EnsureSuccessStatusCode();
            return email;
        }
    }
}
