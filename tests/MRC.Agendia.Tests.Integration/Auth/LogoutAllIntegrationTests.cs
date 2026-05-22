using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Auth
{
    /// <summary>
    /// Coverage of logout-all (issue #59): revoke every active session at once,
    /// idempotently, and revoke them automatically when the password changes.
    /// </summary>
    public class LogoutAllIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string Password = "Test1234!";
        private readonly HttpClient _client;

        public LogoutAllIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task LogoutAll_RevocaTodasLasSesiones_E_EsIdempotente()
        {
            var email = NewEmail();
            var first = await RegisterAsync(email);          // session 1
            var second = await LoginAsync(email);            // session 2

            // logout-all using session 2's access token.
            var response = await PostAuthorizedAsync("/api/auth/logout-all", second.AccessToken);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Both refresh tokens are now revoked.
            Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(first.RefreshToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(second.RefreshToken)).StatusCode);

            // Idempotent: calling it again still succeeds.
            var again = await PostAuthorizedAsync("/api/auth/logout-all", second.AccessToken);
            Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_RevocaTodasLasSesiones()
        {
            var email = NewEmail();
            var first = await RegisterAsync(email);          // session 1
            var second = await LoginAsync(email);            // session 2

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password")
            {
                Content = JsonContent.Create(new ChangePasswordDto(Password, "NewPass5678!"))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", second.AccessToken);
            var changeResponse = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NoContent, changeResponse.StatusCode);

            // Every session (including the one that changed the password) is revoked.
            Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(first.RefreshToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(second.RefreshToken)).StatusCode);

            // The new password works.
            var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto(email, "NewPass5678!"));
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        }

        // ----- Helpers -----

        private static string NewEmail() => $"logout-all-{Guid.NewGuid():N}@agendia.test";

        private async Task<AuthResponseDto> RegisterAsync(string email)
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register/client",
                new RegisterClientDto(email, Password, "Logout All", "600000000"));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        }

        private async Task<AuthResponseDto> LoginAsync(string email)
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto(email, Password));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        }

        private Task<HttpResponseMessage> RefreshAsync(string refreshToken)
            => _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDto(refreshToken));

        private async Task<HttpResponseMessage> PostAuthorizedAsync(string url, string accessToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return await _client.SendAsync(request);
        }
    }
}
