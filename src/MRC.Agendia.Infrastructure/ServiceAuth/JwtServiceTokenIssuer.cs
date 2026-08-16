using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MRC.Agendia.Application.ServiceAuth;

namespace MRC.Agendia.Infrastructure.ServiceAuth
{
    /// <summary>
    /// Signs service tokens with the SAME symmetric key, issuer and audience the
    /// user tokens are validated with (the secret shared with Harmony), so
    /// <c>AuthenticationSetup</c> accepts them unchanged. Emits the SHORT claim
    /// names ("sub", "role") that the inbound claim mapping expects, plus
    /// "client_id" and "token_use=service" to mark it as a machine token.
    /// </summary>
    public class JwtServiceTokenIssuer : IServiceTokenIssuer
    {
        /// <summary>Value of the "token_use" claim on a service token.</summary>
        public const string ServiceTokenUse = "service";

        private readonly string _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly ServiceAuthOptions _options;

        public JwtServiceTokenIssuer(IConfiguration configuration, IOptions<ServiceAuthOptions> options)
        {
            _key = Require(configuration, "Jwt:Key");
            _issuer = Require(configuration, "Jwt:Issuer");
            _audience = Require(configuration, "Jwt:Audience");
            _options = options.Value;
        }

        /// <inheritdoc />
        public ServiceToken Issue(AuthenticatedServiceClient client)
        {
            // Token lifetime is a real instant, so it is anchored to UTC (unlike the
            // wall-clock appointment times). exp/nbf/iat all derive from this.
            var now = DateTime.UtcNow;
            var expiresAt = now.AddMinutes(_options.TokenLifetimeMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, client.ClientId),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("client_id", client.ClientId),
                new("token_use", ServiceTokenUse),
                new("role", client.Role)
            };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(issuer: _issuer,
                                             audience: _audience,
                                             claims: claims,
                                             notBefore: now,
                                             expires: expiresAt,
                                             signingCredentials: credentials);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            return new ServiceToken(accessToken, expiresAt);
        }

        private static string Require(IConfiguration configuration, string key)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"{key} is not configured.");
            return value;
        }
    }
}
