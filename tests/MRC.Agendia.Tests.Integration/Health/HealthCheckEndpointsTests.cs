using System.Net;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Health
{
    /// <summary>
    /// Smoke coverage for the health endpoints (issue #62). /health/live has no
    /// dependency checks, so it is deterministic in the Testing environment
    /// (no real SQL/Seq needed) and proves the endpoints are wired.
    /// </summary>
    public class HealthCheckEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public HealthCheckEndpointsTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Live_DevuelveHealthy_200()
        {
            var response = await _client.GetAsync("/health/live");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
