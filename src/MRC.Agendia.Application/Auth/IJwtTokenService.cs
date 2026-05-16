using System.Security.Claims;

namespace MRC.Agendia.Application.Auth
{
    public interface IJwtTokenService
    {
        (string token, DateTime expiresAt) GenerateAccessToken(string userId, string email, string fullName, IEnumerable<string> roles, IEnumerable<Claim>? extraClaims = null);
        string GenerateRefreshToken();
    }
}
