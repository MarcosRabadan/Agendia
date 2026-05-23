using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth
{
    /// <summary>
    /// Account creation for the three user kinds (Client, Owner, Employee),
    /// including the associated domain entity and the confirmation email.
    /// Split out of <see cref="IAuthService"/> so credential management and user
    /// registration are separate responsibilities.
    /// </summary>
    public interface IUserRegistrationService
    {
        Task<AuthResponseDto> RegisterClientAsync(RegisterClientDto dto, CancellationToken cancellationToken = default);
        Task<AuthResponseDto> RegisterOwnerAsync(RegisterOwnerDto dto, CancellationToken cancellationToken = default);
        Task<UserDto> RegisterEmployeeAsync(RegisterEmployeeDto dto, string currentOwnerUserId, CancellationToken cancellationToken = default);
    }
}
