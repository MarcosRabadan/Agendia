using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth
{
    /// <summary>
    /// Credential and account management. User registration lives in
    /// <see cref="IUserRegistrationService"/>.
    /// </summary>
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> RefreshAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
        Task LogoutAllAsync(string userId);
        Task ChangePasswordAsync(string userId, ChangePasswordDto dto);
        Task ForgotPasswordAsync(ForgotPasswordDto dto);
        Task ResetPasswordAsync(ResetPasswordDto dto);
        Task ConfirmEmailAsync(ConfirmEmailDto dto);
        Task<UserDto> GetCurrentUserAsync(string userId);
    }
}
