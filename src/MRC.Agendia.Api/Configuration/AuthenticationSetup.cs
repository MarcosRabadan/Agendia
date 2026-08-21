using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MRC.Agendia.Api.Services;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Api.Configuration
{
    /// <summary>
    /// Configures JWT Bearer authentication.
    ///
    /// Agendia is a downstream microservice: it does NOT issue tokens. The Harmony
    /// identity service owns users and credentials and signs the access tokens;
    /// Agendia only validates them and reads the caller's identity from the claims.
    /// The signing key (Jwt:Key) is therefore a shared secret owned by Harmony,
    /// used here purely for verification.
    /// </summary>
    public static class AuthenticationSetup
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtKey = ValidateAndGetJwtKey(configuration);
            // Issuer/Audience are validated too: with ValidateIssuer/Audience on, a
            // missing value does not disable the check, it makes EVERY request fail
            // at runtime. Fail at startup instead, where the cause is obvious.
            var jwtIssuer = RequireValue(configuration, "Jwt:Issuer");
            var jwtAudience = RequireValue(configuration, "Jwt:Audience");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = true;

                // Inbound claim mapping is left ON *deliberately*, and set explicitly
                // rather than relying on the framework default. Harmony emits the short
                // JWT claim names ("sub" and "role"); the mapping translates them to the
                // long ClaimTypes URIs that ICurrentUserContext and every [Authorize]
                // attribute in this service read. Turning this off would silently make
                // ICurrentUserContext.UserId null and every authorization check return
                // 403 - see docs/harmony-token-contract.md.
                options.MapInboundClaims = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    // Pin the signing algorithm so a token forged with a different alg
                    // (e.g. "none" or an asymmetric alg) cannot be accepted.
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    // Claim types the authorization layer reads, post-mapping.
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

            services.AddAuthorization();

            // Resource-based authorization helpers (need HttpContext to read the caller).
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserContext, CurrentUserContext>();

            return services;
        }

        /// <summary>Fail-fast read of a required Jwt setting that must match Harmony's.</summary>
        private static string RequireValue(IConfiguration configuration, string key)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"{key} is not configured. It must exactly match the value Harmony issues " +
                    "in the token (see docs/harmony-token-contract.md).");
            }
            return value;
        }

        /// <summary>Fail-fast validation: the app does NOT start if Jwt:Key is missing or too short.</summary>
        private static string ValidateAndGetJwtKey(IConfiguration configuration)
        {
            var jwtKey = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException(
                    "Jwt:Key is not configured. It is the symmetric key shared with Harmony, " +
                    "which signs the tokens this service validates; it must be identical on both sides.\n" +
                    "In development set it with:\n" +
                    "  dotnet user-secrets --project src/MRC.Agendia.Api set \"Jwt:Key\" \"<Harmony key>\"\n" +
                    "In production use a Jwt__Key environment variable.");
            }
            // Validate the actual key size in bytes (HS256 needs >= 256 bits = 32 bytes),
            // not just the character count, so a short multi-byte key cannot slip through.
            if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Key is too short: HS256 needs at least 32 bytes. " +
                    "It is the symmetric key shared with Harmony, so lengthen it on both sides.");
            }
            return jwtKey;
        }
    }
}
