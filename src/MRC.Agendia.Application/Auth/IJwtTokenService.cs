namespace MRC.Agendia.Application.Auth
{
    public interface IJwtTokenService
    {
        /// <summary>Builds a signed JWT access token carrying the user's identity and role claims.</summary>
        /// <param name="userId">The user id placed in the subject and name-identifier claims.</param>
        /// <param name="email">The user's email claim.</param>
        /// <param name="fullName">The user's display name claim.</param>
        /// <param name="roles">The roles to add as role claims.</param>
        /// <returns>The serialized access token and its UTC expiry.</returns>
        (string token, DateTime expiresAt) GenerateAccessToken(string userId, string email, string fullName, IEnumerable<string> roles);

        /// <summary>Generates a cryptographically random opaque refresh token (cleartext, to be returned to the client).</summary>
        /// <returns>A new random refresh token string.</returns>
        string GenerateRefreshToken();
    }
}
