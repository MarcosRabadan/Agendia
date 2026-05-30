namespace MRC.Agendia.Infrastructure.Identity
{
    /// <summary>
    /// Composes and sends the account emails (confirmation, password reset):
    /// token generation, building the frontend link and the message body. Shared
    /// by the registration service and the auth service.
    /// </summary>
    public interface IAuthEmailService
    {
        /// <summary>Generates an email-confirmation token and sends the user the confirmation link.</summary>
        /// <param name="user">The user to send the confirmation email to.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task SendEmailConfirmationAsync(ApplicationUser user, CancellationToken cancellationToken = default);

        /// <summary>Generates a password-reset token and sends the user the reset link.</summary>
        /// <param name="user">The user to send the reset email to.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task SendPasswordResetAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    }
}
