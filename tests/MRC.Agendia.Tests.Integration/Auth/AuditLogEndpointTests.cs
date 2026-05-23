using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Auth
{
    /// <summary>
    /// Authorization coverage for the admin audit-log endpoint (issue #56):
    /// only Admin can read it.
    /// </summary>
    public class AuditLogEndpointTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public AuditLogEndpointTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAuditLogs_SinToken_401()
        {
            var response = await _client.GetAsync("/api/admin/audit-logs");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAuditLogs_ComoClient_403()
        {
            var email = $"audit-{Guid.NewGuid():N}@agendia.test";
            var register = await _client.PostAsJsonAsync("/api/auth/register/client",
                new RegisterClientDto(email, "Test1234!", "Audit User", "600000000"));
            register.EnsureSuccessStatusCode();
            var auth = await register.Content.ReadFromJsonAsync<AuthResponseDto>();

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/audit-logs");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
