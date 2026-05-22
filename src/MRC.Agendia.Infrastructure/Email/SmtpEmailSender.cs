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

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var smtp = _configuration.GetSection("Email:Smtp");
            var host = smtp["Host"]!;
            var port = int.TryParse(smtp["Port"], out var p) ? p : 587;
            var user = smtp["User"];
            var password = smtp["Password"];
            var enableSsl = !bool.TryParse(smtp["EnableSsl"], out var ssl) || ssl;
            var from = smtp["From"]!;
            var fromName = smtp["FromName"] ?? "Agendia";

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
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

            await client.SendMailAsync(message);
        }
    }
}
