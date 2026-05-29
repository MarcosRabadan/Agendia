using MRC.Agendia.Application.Common.Push;

namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>
    /// Captures the push notifications the app would send so tests can assert on
    /// them instead of hitting a real provider. Registered in
    /// <see cref="CustomWebApplicationFactory"/>. Mirrors <see cref="FakeEmailSender"/>.
    /// </summary>
    public class FakePushSender : IPushSender
    {
        private readonly object _gate = new();
        private readonly List<SentPush> _sent = new();

        public Task SendAsync(
            IReadOnlyCollection<string> deviceTokens,
            string title,
            string body,
            IReadOnlyDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _sent.Add(new SentPush(deviceTokens.ToList(), title, body, data));
            }
            return Task.CompletedTask;
        }

        /// <summary>Polls until a push targeting the given token is captured, or times out.</summary>
        public async Task<SentPush?> WaitForTokenAsync(string token, int timeoutMs = 2000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            SentPush? found;
            while ((found = LastWithToken(token)) is null && DateTime.UtcNow < deadline)
                await Task.Delay(25);
            return found;
        }

        private SentPush? LastWithToken(string token)
        {
            lock (_gate)
            {
                return _sent.LastOrDefault(p => p.Tokens.Contains(token));
            }
        }
    }

    public record SentPush(IReadOnlyList<string> Tokens, string Title, string Body, IReadOnlyDictionary<string, string>? Data);
}
