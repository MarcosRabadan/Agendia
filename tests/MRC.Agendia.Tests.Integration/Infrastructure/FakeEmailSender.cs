using System.Text.RegularExpressions;
using MRC.Agendia.Application.Common.Email;

namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>
    /// Captures the emails the auth flow would send so tests can read the
    /// reset/confirmation token out of the body instead of hitting a real SMTP
    /// server. Registered in <see cref="CustomWebApplicationFactory"/>.
    /// </summary>
    public class FakeEmailSender : IEmailSender
    {
        private readonly object _gate = new();
        private readonly List<SentEmail> _sent = new();

        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _sent.Add(new SentEmail(toEmail, subject, htmlBody));
            }
            return Task.CompletedTask;
        }

        /// <summary>Last email captured for the given recipient, or null.</summary>
        public SentEmail? LastTo(string email)
        {
            lock (_gate)
            {
                return _sent.LastOrDefault(e =>
                    string.Equals(e.To, email, StringComparison.OrdinalIgnoreCase));
            }
        }

        public bool AnyTo(string email)
        {
            lock (_gate)
            {
                return _sent.Any(e =>
                    string.Equals(e.To, email, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Polls until an email to the recipient (optionally matching a predicate,
        /// e.g. a subject) is captured, or the timeout elapses. Keeps tests
        /// deterministic when the send runs in the background. Returns the last
        /// matching email, or null. A predicate avoids matching an earlier email
        /// to the same address (e.g. the confirmation email before a reset).
        /// </summary>
        public async Task<SentEmail?> WaitForAsync(string email, Func<SentEmail, bool>? match = null, int timeoutMs = 2000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            SentEmail? found;
            while ((found = LastMatching(email, match)) is null && DateTime.UtcNow < deadline)
                await Task.Delay(25);
            return found;
        }

        private SentEmail? LastMatching(string email, Func<SentEmail, bool>? match)
        {
            lock (_gate)
            {
                return _sent.LastOrDefault(e =>
                    string.Equals(e.To, email, StringComparison.OrdinalIgnoreCase)
                    && (match is null || match(e)));
            }
        }
    }

    public record SentEmail(string To, string Subject, string Body)
    {
        /// <summary>Extracts and URL-decodes a query parameter from the link in the body.</summary>
        public string? QueryValue(string key)
        {
            var match = Regex.Match(Body, $@"[?&]{Regex.Escape(key)}=([^""&]+)");
            return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : null;
        }
    }
}
