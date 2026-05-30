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
        /// <summary>Creates a Client user with its associated Client entity in a single transaction and sends the confirmation email.</summary>
        /// <param name="dto">The client account details (name, email, phone, password).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The created user with a session (or without one when email confirmation is required).</returns>
        Task<AuthResponseDto> RegisterClientAsync(RegisterClientDto dto, CancellationToken cancellationToken = default);

        /// <summary>Creates a BusinessOwner user together with its Business and an auto-created owner Employee in a single transaction, then sends the confirmation email.</summary>
        /// <param name="dto">The owner account and business details.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The created user with a session (or without one when email confirmation is required).</returns>
        Task<AuthResponseDto> RegisterOwnerAsync(RegisterOwnerDto dto, CancellationToken cancellationToken = default);

        /// <summary>Creates an Employee user for a business the caller owns and sends the confirmation email. No session is issued.</summary>
        /// <param name="dto">The employee account details, including the target business id.</param>
        /// <param name="currentOwnerUserId">The id of the owner making the request; must own the business.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The created employee user and their roles.</returns>
        Task<UserDto> RegisterEmployeeAsync(RegisterEmployeeDto dto, string currentOwnerUserId, CancellationToken cancellationToken = default);
    }
}
