using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Identity;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Auth
{
    /// <summary>
    /// End-to-end check that refresh tokens are persisted hashed (#195): the cleartext
    /// value is returned to the client but never stored, so a DB leak exposes no
    /// reusable tokens. The full login/refresh/logout flow is covered by AuthFlow.
    /// </summary>
    public class AuthRefreshTokenHashingIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AuthRefreshTokenHashingIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Register_StoresHashedRefreshToken_NotCleartext()
        {
            var email = $"hash-{Guid.NewGuid():N}@agendia.test";
            var dto = new RegisterClientDto(email, "Client1234!", "Ana", "600111222");

            var response = await _client.PostAsJsonAsync("/api/auth/register/client", dto);
            response.EnsureSuccessStatusCode();
            var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            var cleartext = auth!.RefreshToken;
            Assert.False(string.IsNullOrWhiteSpace(cleartext));

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

            // The cleartext token is NOT in the DB; its hash IS.
            Assert.False(await db.RefreshTokens.AnyAsync(rt => rt.Token == cleartext));
            Assert.True(await db.RefreshTokens.AnyAsync(rt => rt.Token == RefreshTokenHasher.Hash(cleartext)));
        }
    }
}
