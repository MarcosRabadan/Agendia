using Microsoft.Extensions.Logging;
using MRC.Agendia.Application.Common.Email;

namespace MRC.Agendia.Infrastructure.Email
{
    /// <summary>
    /// Development/Testing email sender. It does not send anything: it writes the
    /// recipient, subject and body (which contains the reset/confirmation link)
    /// to the log so a developer can grab the token from Seq without an SMTP
    /// server. Never used in Production - see EmailSetup for the wiring.
    /// </summary>
    public class LoggingEmailSender : IEmailSender
    {
        private readonly ILogger<LoggingEmailSender> _logger;

        public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            _logger.LogInformation(
                "[DEV EMAIL] To: {ToEmail} | Subject: {Subject}\n{Body}",
                toEmail, subject, htmlBody);
            return Task.CompletedTask;
        }
    }
}
