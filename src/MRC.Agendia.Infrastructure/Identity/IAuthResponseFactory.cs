using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Infrastructure.Identity
{
    /// <summary>
    /// Builds the access + refresh token pair, persists the refresh token and
    /// wraps everything in an <see cref="AuthResponseDto"/>. Shared by
    /// registration, login and refresh so the token-issuing logic lives in one
    /// place. Lives in Infrastructure because it operates on
    /// <see cref="ApplicationUser"/>.
    /// </summary>
    public interface IAuthResponseFactory
    {
        Task<AuthResponseDto> CreateAsync(ApplicationUser user, string? existingRefreshToken = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Builds a response with the user but WITHOUT a session (empty tokens, no
        /// refresh token persisted). Used when email confirmation is required: no
        /// session is granted until the user confirms and logs in.
        /// </summary>
        Task<AuthResponseDto> CreateWithoutSessionAsync(ApplicationUser user);
    }
}
