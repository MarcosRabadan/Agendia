using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterClientAsync(RegisterClientDto dto);
        Task<AuthResponseDto> RegisterOwnerAsync(RegisterOwnerDto dto);
        Task<UserDto> RegisterEmployeeAsync(RegisterEmployeeDto dto, string currentOwnerUserId);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> RefreshAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
        Task ChangePasswordAsync(string userId, ChangePasswordDto dto);
        Task<UserDto> GetCurrentUserAsync(string userId);
    }
}
