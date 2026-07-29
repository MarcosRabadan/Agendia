using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MRC.Agendia.Application.ServiceAuth;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Infrastructure.ServiceAuth;

namespace MRC.Agendia.Tests.Unit.Infrastructure.ServiceAuth
{
    /// <summary>
    /// Unit tests for the JWT service-token issuer: the emitted token carries the
    /// short claim names Harmony's validation expects (sub/role) plus the
    /// service markers, and it validates against the same key/issuer/audience.
    /// </summary>
    public class JwtServiceTokenIssuerTests
    {
        private const string Key = "unit-test-signing-key-that-is-long-enough-1234567890";
        private const string Issuer = "MRC.Agendia";
        private const string Audience = "MRC.Agendia.Clients";

        private static JwtServiceTokenIssuer BuildIssuer(int lifetimeMinutes = 15)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = Key,
                    ["Jwt:Issuer"] = Issuer,
                    ["Jwt:Audience"] = Audience
                })
                .Build();

            var options = Options.Create(new ServiceAuthOptions { TokenLifetimeMinutes = lifetimeMinutes });
            return new JwtServiceTokenIssuer(config, options);
        }

        [Fact]
        public void Issued_token_carries_the_expected_service_claims()
        {
            var before = DateTime.UtcNow;
            var token = BuildIssuer(lifetimeMinutes: 20).Issue(new AuthenticatedServiceClient("soundmate", Roles.Admin));

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);

            Assert.Equal("soundmate", jwt.Claims.Single(c => c.Type == "sub").Value);
            Assert.Equal("soundmate", jwt.Claims.Single(c => c.Type == "client_id").Value);
            Assert.Equal("service", jwt.Claims.Single(c => c.Type == "token_use").Value);
            Assert.Equal(Roles.Admin, jwt.Claims.Single(c => c.Type == "role").Value);
            Assert.Equal(Issuer, jwt.Issuer);
            Assert.Contains(Audience, jwt.Audiences);

            // Expiry is ~20 minutes out and reported back as UTC.
            Assert.Equal(DateTimeKind.Utc, token.ExpiresAtUtc.Kind);
            Assert.InRange(token.ExpiresAtUtc, before.AddMinutes(19), before.AddMinutes(21));
        }

        [Fact]
        public void Issued_token_validates_against_the_same_signing_parameters()
        {
            var token = BuildIssuer().Issue(new AuthenticatedServiceClient("soundmate", Roles.Admin));

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = Issuer,
                ValidAudience = Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.NameIdentifier
            };

            var principal = new JwtSecurityTokenHandler().ValidateToken(token.AccessToken, parameters, out _);

            // Inbound mapping turns "sub"/"role" into the long ClaimTypes the app reads.
            Assert.Equal("soundmate", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.True(principal.IsInRole(Roles.Admin));
        }
    }
}
