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
        Task<AuthResponseDto> CreateAsync(ApplicationUser user, string? existingRefreshToken = null);
    }
}
