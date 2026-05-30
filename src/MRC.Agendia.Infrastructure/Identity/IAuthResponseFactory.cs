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
        /// <summary>Builds an access + refresh token pair, persists the refresh token and wraps it all in an auth response.</summary>
        /// <param name="user">The user the tokens are issued for.</param>
        /// <param name="existingRefreshToken">A refresh token value to reuse (e.g. the rotated one from a refresh); when null a new one is generated.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The user with a fresh access and refresh token.</returns>
        Task<AuthResponseDto> CreateAsync(ApplicationUser user, string? existingRefreshToken = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Builds a response with the user but WITHOUT a session (empty tokens, no
        /// refresh token persisted). Used when email confirmation is required: no
        /// session is granted until the user confirms and logs in.
        /// </summary>
        /// <param name="user">The user to wrap in the response.</param>
        /// <returns>The user with empty access and refresh tokens.</returns>
        Task<AuthResponseDto> CreateWithoutSessionAsync(ApplicationUser user);
    }
}
