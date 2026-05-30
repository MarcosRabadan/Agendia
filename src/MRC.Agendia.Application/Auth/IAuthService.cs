using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth
{
    /// <summary>
    /// Credential and account management. User registration lives in
    /// <see cref="IUserRegistrationService"/>.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>Validates the credentials and issues an access + refresh token pair. Enforces lockout, inactive-account and (when enabled) email-confirmation gates.</summary>
        /// <param name="dto">The email and password to authenticate.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The authenticated user with a fresh access and refresh token.</returns>
        Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);

        /// <summary>Rotates a valid refresh token: revokes the presented one and issues a new pair. Reusing an already-rotated token revokes the whole session family (reuse detection).</summary>
        /// <param name="refreshToken">The current refresh token to exchange.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A new access and refresh token for the user.</returns>
        Task<AuthResponseDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

        /// <summary>Revokes a single refresh token. Idempotent, and only revokes the token when it belongs to the given user.</summary>
        /// <param name="refreshToken">The refresh token to revoke.</param>
        /// <param name="userId">The id of the user that owns the token.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task LogoutAsync(string refreshToken, string userId, CancellationToken cancellationToken = default);

        /// <summary>Revokes every active refresh token for the user, ending all their sessions.</summary>
        /// <param name="userId">The id of the user whose sessions are revoked.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task LogoutAllAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>Changes the user's password after verifying the current one, then revokes all their other sessions.</summary>
        /// <param name="userId">The id of the user changing the password.</param>
        /// <param name="dto">The current and new password.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task ChangePasswordAsync(string userId, ChangePasswordDto dto, CancellationToken cancellationToken = default);

        /// <summary>Triggers a password-reset email. Anti-enumeration: returns immediately and only sends the email when the account exists and is active.</summary>
        /// <param name="dto">The email to send the reset link to.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default);

        /// <summary>Sets a new password using the emailed reset token, clears any lockout and revokes all the user's sessions.</summary>
        /// <param name="dto">The email, reset token and new password.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default);

        /// <summary>Confirms the user's email address using the token sent at registration.</summary>
        /// <param name="dto">The user id and confirmation token.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task ConfirmEmailAsync(ConfirmEmailDto dto, CancellationToken cancellationToken = default);

        /// <summary>Returns the profile of the authenticated user, including their roles.</summary>
        /// <param name="userId">The id of the user to load.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The user's profile and assigned roles.</returns>
        Task<UserDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default);
    }
}
