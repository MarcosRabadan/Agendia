using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth
{
    /// <summary>
    /// Credential and account management. User registration lives in
    /// <see cref="IUserRegistrationService"/>.
    /// </summary>
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
        Task<AuthResponseDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task LogoutAllAsync(string userId, CancellationToken cancellationToken = default);
        Task ChangePasswordAsync(string userId, ChangePasswordDto dto, CancellationToken cancellationToken = default);
        Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default);
        Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default);
        Task ConfirmEmailAsync(ConfirmEmailDto dto, CancellationToken cancellationToken = default);
        Task<UserDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default);
    }
}
