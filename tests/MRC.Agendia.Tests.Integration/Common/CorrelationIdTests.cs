using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Common
{
    /// <summary>
    /// Coverage for the correlation id middleware (issue #61). Uses the anonymous
    /// /health/live endpoint so it does not depend on auth or real dependencies.
    /// </summary>
    public class CorrelationIdTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string HeaderName = "X-Correlation-Id";
        private readonly HttpClient _client;

        public CorrelationIdTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Response_IncludeCorrelationId_CuandoNoSeEnvia()
        {
            var response = await _client.GetAsync("/health/live");

            Assert.True(response.Headers.Contains(HeaderName));
            var value = response.Headers.GetValues(HeaderName).Single();
            Assert.False(string.IsNullOrWhiteSpace(value));
        }

        [Fact]
        public async Task Response_DevuelveElMismoCorrelationId_QueSeEnvia()
        {
            var provided = "test-correlation-123";

            using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
            request.Headers.Add(HeaderName, provided);
            var response = await _client.SendAsync(request);

            Assert.True(response.Headers.Contains(HeaderName));
            Assert.Equal(provided, response.Headers.GetValues(HeaderName).Single());
        }
    }
}
