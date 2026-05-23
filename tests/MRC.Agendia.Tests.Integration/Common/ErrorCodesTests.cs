using System.Net;
using System.Net.Http.Json;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Common
{
    /// <summary>
    /// Verifies the middleware surfaces specific, machine-readable error codes
    /// (issue #60) instead of the generic ones.
    /// </summary>
    public class ErrorCodesTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string Password = "Test1234!";
        private readonly HttpClient _client;

        public ErrorCodesTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task RegisterClient_EmailDuplicado_DevuelveCodeDuplicateEmail()
        {
            var email = $"dup-{Guid.NewGuid():N}@agendia.test";
            var dto = new RegisterClientDto(email, Password, "Dup User", "600000000");

            var first = await _client.PostAsJsonAsync("/api/auth/register/client", dto);
            first.EnsureSuccessStatusCode();

            var second = await _client.PostAsJsonAsync("/api/auth/register/client", dto);

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
            var body = await second.Content.ReadAsStringAsync();
            Assert.Contains("\"code\":\"DUPLICATE_EMAIL\"", body);
        }
    }
}
