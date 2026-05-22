namespace MRC.Agendia.Application.Common.Email
{
    /// <summary>
    /// Transactional email abstraction. Implementations live in Infrastructure
    /// (SMTP for real delivery, logging for dev/test). Kept intentionally small
    /// so the future notifications system (issue #51) can reuse it.
    /// </summary>
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }
}
