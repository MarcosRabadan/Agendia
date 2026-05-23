namespace MRC.Agendia.Infrastructure.Identity
{
    /// <summary>
    /// Composes and sends the account emails (confirmation, password reset):
    /// token generation, building the frontend link and the message body. Shared
    /// by the registration service and the auth service.
    /// </summary>
    public interface IAuthEmailService
    {
        Task SendEmailConfirmationAsync(ApplicationUser user, CancellationToken cancellationToken = default);
        Task SendPasswordResetAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    }
}
