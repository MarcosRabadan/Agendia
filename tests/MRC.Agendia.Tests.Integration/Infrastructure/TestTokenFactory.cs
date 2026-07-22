using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>
    /// Forges the access tokens that the Harmony identity service would issue.
    ///
    /// Agendia no longer registers users or issues tokens, so integration tests
    /// cannot obtain one by calling an endpoint. They mint one here instead, which
    /// is both faster and more faithful: the token carries the SHORT claim names
    /// ("sub", "role") exactly as Harmony emits them, so the suite exercises the
    /// real inbound claim mapping rather than a shape only these tests produce.
    ///
    /// The signing material mirrors what CustomWebApplicationFactory feeds the host.
    /// </summary>
    public static class TestTokenFactory
    {
        public const string Key = "test-key-for-integration-tests-do-not-use-in-production-1234567890";
        public const string Issuer = "MRC.Agendia.Tests";
        public const string Audience = "MRC.Agendia.Tests.Clients";

        /// <summary>Builds a bearer token for the given Harmony user id and roles.</summary>
        public static string Create(string userId, params string[] roles) =>
            CreateCustom(userId, roles);

        /// <summary>
        /// Builds a token with individual pieces of the contract overridden, so the
        /// contract tests can assert that Agendia REJECTS each malformed variant.
        /// A null <paramref name="userId"/> omits the "sub" claim entirely.
        /// </summary>
        public static string CreateCustom(string? userId,
                                          string[]? roles = null,
                                          string? issuer = null,
                                          string? audience = null,
                                          string? signingKey = null,
                                          DateTime? expires = null)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // "sub" is what Harmony emits; the JwtBearer inbound map turns it into
            // ClaimTypes.NameIdentifier, which is what ICurrentUserContext reads.
            if (userId is not null)
                claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userId));

            claims.AddRange((roles ?? Array.Empty<string>()).Select(role => new Claim("role", role)));

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey ?? Key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(issuer: issuer ?? Issuer,
                                             audience: audience ?? Audience,
                                             claims: claims,
                                             expires: expires ?? DateTime.UtcNow.AddMinutes(15),
                                             signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
