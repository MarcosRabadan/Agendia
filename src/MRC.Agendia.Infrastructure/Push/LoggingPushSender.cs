using Microsoft.Extensions.Logging;
using MRC.Agendia.Application.Common.Push;

namespace MRC.Agendia.Infrastructure.Push
{
    /// <summary>
    /// Development/Testing push sender. It does not deliver anything: it writes the
    /// target device count, title and body to the log. The real provider (e.g. FCM)
    /// is wired separately - see PushSetup. Mirrors LoggingEmailSender.
    /// </summary>
    public class LoggingPushSender : IPushSender
    {
        private readonly ILogger<LoggingPushSender> _logger;

        public LoggingPushSender(ILogger<LoggingPushSender> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task SendAsync(
            IReadOnlyCollection<string> deviceTokens,
            string title,
            string body,
            IReadOnlyDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
        {
            if (deviceTokens is null || deviceTokens.Count == 0)
                return Task.CompletedTask;

            _logger.LogInformation(
                "[DEV PUSH] To {Count} device(s) | Title: {Title}\n{Body}",
                deviceTokens.Count, title, body);
            return Task.CompletedTask;
        }
    }
}
