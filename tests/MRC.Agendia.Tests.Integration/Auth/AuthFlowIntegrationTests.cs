using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Auth
{
    /// <summary>
    /// End-to-end coverage of the auth flow: register, login, /me, refresh, logout
    /// plus the credential/lockout edge cases listed in issue #47.
    ///
    /// Notes on status codes:
    ///   - GET /me without token: 401 (rejected by ASP.NET auth pipeline before
    ///     reaching ExceptionHandlingMiddleware).
    ///   - Login with wrong credentials / locked account: 401 (AuthService
    ///     throws AuthenticationException, which the middleware maps to 401).
    ///   - Refresh with revoked/invalid token: 401 (same reason).
    ///   - Resource-based authorization failures (covered in #46) still return
    ///     403 because they throw UnauthorizedAccessException - that is the
    ///     correct semantic for "authenticated but not allowed".
    /// </summary>
    public class AuthFlowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string ValidPassword = "Test1234!";
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AuthFlowIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        // ===================================================================
        //  Register
        // ===================================================================

        [Fact]
        public async Task RegisterClient_DevuelveTokens()
        {
            var email = NewEmail();

            var response = await _client.PostAsJsonAsync("/api/auth/register/client",
                new RegisterClientDto(email, ValidPassword, "Cliente Uno", "600000001"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(body);
            Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
            Assert.Equal(email, body.User.Email);
            Assert.Contains("Client", body.User.Roles);
        }

        // ===================================================================
        //  Login
        // ===================================================================

        [Fact]
        public async Task Login_CredencialesValidas_DevuelveTokens()
        {
            var (email, _) = await RegisterUserAsync();

            var response = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginDto(email, ValidPassword));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(body);
            Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
        }

        [Fact]
        public async Task Login_CredencialesInvalidas_401()
        {
            var (email, _) = await RegisterUserAsync();

            var response = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginDto(email, "WrongPassword1!"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("Credenciales invalidas.", body);
        }

        [Fact]
        public async Task Login_EmailInexistente_401()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginDto($"missing-{Guid.NewGuid():N}@test.local", ValidPassword));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ===================================================================
        //  Lockout
        // ===================================================================

        [Fact]
        public async Task Lockout_TrasCincoFallosBloqueaLaCuenta()
        {
            var (email, _) = await RegisterUserAsync();

            // 5 failed attempts trigger Identity's lockout (configured in
            // AuthenticationSetup: MaxFailedAccessAttempts = 5).
            for (var i = 0; i < 5; i++)
            {
                var fail = await _client.PostAsJsonAsync("/api/auth/login",
                    new LoginDto(email, "WrongPassword1!"));
                Assert.Equal(HttpStatusCode.Unauthorized, fail.StatusCode);
            }

            // Even with the correct password the account is now locked out.
            var locked = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginDto(email, ValidPassword));

            Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
            var body = await locked.Content.ReadAsStringAsync();
            Assert.Contains("Cuenta bloqueada", body);
        }

        // ===================================================================
        //  GET /me
        // ===================================================================

        [Fact]
        public async Task Me_ConAccessTokenValido_200()
        {
            var (email, tokens) = await RegisterUserAsync();

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var me = await response.Content.ReadFromJsonAsync<UserDto>();
            Assert.NotNull(me);
            Assert.Equal(email, me!.Email);
            Assert.Contains("Client", me.Roles);
        }

        [Fact]
        public async Task Me_SinToken_401()
        {
            // Bare /me request without Authorization header -> ASP.NET auth
            // pipeline rejects with 401 BEFORE reaching the middleware that
            // would otherwise map UnauthorizedAccessException to 403.
            var response = await _client.GetAsync("/api/auth/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ===================================================================
        //  Refresh token rotation
        // ===================================================================

        [Fact]
        public async Task Refresh_DevuelveNuevoToken_E_InvalidaElAnterior()
        {
            var (_, tokens) = await RegisterUserAsync();
            var originalRefresh = tokens.RefreshToken;

            // First refresh succeeds and returns a new token.
            var firstRefresh = await _client.PostAsJsonAsync("/api/auth/refresh",
                new RefreshTokenRequestDto(originalRefresh));

            Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);
            var rotated = await firstRefresh.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(rotated);
            Assert.NotEqual(originalRefresh, rotated!.RefreshToken);

            // Second use of the ORIGINAL token must fail (it was revoked when
            // the first refresh rotated it).
            var reuse = await _client.PostAsJsonAsync("/api/auth/refresh",
                new RefreshTokenRequestDto(originalRefresh));

            Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        }

        // ===================================================================
        //  Logout
        // ===================================================================

        [Fact]
        public async Task Logout_RevocaElRefreshToken()
        {
            var (_, tokens) = await RegisterUserAsync();

            using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
            {
                Content = JsonContent.Create(new LogoutRequestDto(tokens.RefreshToken))
            };
            logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            var logoutResponse = await _client.SendAsync(logoutRequest);

            Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

            // Using the refresh token after logout must fail.
            var afterLogout = await _client.PostAsJsonAsync("/api/auth/refresh",
                new RefreshTokenRequestDto(tokens.RefreshToken));

            Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
        }

        // ===================================================================
        //  Helpers
        // ===================================================================

        private static string NewEmail() => $"test-{Guid.NewGuid():N}@agendia.test";

        private async Task<(string Email, AuthResponseDto Tokens)> RegisterUserAsync()
        {
            var email = NewEmail();
            var response = await _client.PostAsJsonAsync("/api/auth/register/client",
                new RegisterClientDto(email, ValidPassword, "Test User", "600000000"));
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return (email, body!);
        }
    }
}
