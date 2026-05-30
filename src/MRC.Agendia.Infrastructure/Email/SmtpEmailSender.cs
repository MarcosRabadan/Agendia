using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using MRC.Agendia.Application.Common.Email;

namespace MRC.Agendia.Infrastructure.Email
{
    /// <summary>
    /// Provider-agnostic SMTP email sender. Works against any SMTP relay
    /// (Amazon SES, SendGrid, Mailgun, Gmail, Mailtrap, a corporate server...),
    /// so the project is not locked to a vendor SDK while the cloud is undecided.
    ///
    /// Configuration (Email:Smtp:*):
    ///   Host, Port, User, Password, EnableSsl, From, FromName
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <inheritdoc />
        public async Task SendAsync(string toEmail,
                                    string subject,
                                    string htmlBody,
                                    CancellationToken cancellationToken = default)
        {
            var smtp = _configuration.GetSection("Email:Smtp");
            var host = smtp["Host"]!;
            var port = int.TryParse(smtp["Port"], out var p) ? p : 587;
            var user = smtp["User"];
            var password = smtp["Password"];
            var enableSsl = !bool.TryParse(smtp["EnableSsl"], out var ssl) || ssl;
            var from = smtp["From"]!;
            var fromName = smtp["FromName"] ?? "Agendia";
            // Bound the wait on a hung relay (SmtpClient defaults to 100s). Matters most
            // for the reset/confirm emails, awaited on the request thread (not best-effort).
            var timeoutSeconds = int.TryParse(smtp["TimeoutSeconds"], out var ts) && ts > 0 ? ts : 15;

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = timeoutSeconds * 1000
            };

            if (!string.IsNullOrWhiteSpace(user))
                client.Credentials = new NetworkCredential(user, password);

            using var message = new MailMessage
            {
                From = new MailAddress(from, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            // SmtpClient.Timeout governs the synchronous internals; for SendMailAsync the
            // token is what actually bounds a hung relay. Link the caller's token with a
            // CancelAfter so a stuck send fails within the timeout instead of waiting ~100s.
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            await client.SendMailAsync(message, timeoutCts.Token);
        }
    }
}
