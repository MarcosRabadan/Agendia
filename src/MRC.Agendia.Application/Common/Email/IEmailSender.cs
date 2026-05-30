namespace MRC.Agendia.Application.Common.Email
{
    /// <summary>
    /// Transactional email abstraction. Implementations live in Infrastructure
    /// (SMTP for real delivery, logging for dev/test). Kept intentionally small
    /// so the future notifications system (issue #51) can reuse it.
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>Sends an HTML email to a single recipient.</summary>
        /// <param name="toEmail">Recipient email address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="htmlBody">HTML body of the email.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
    }
}
