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
                    $"{key} no esta configurado. Debe coincidir exactamente con el valor que emite Harmony " +
                    "en el token (ver docs/harmony-token-contract.md).");
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
                    "Jwt:Key no esta configurado. Es la clave simetrica compartida con Harmony, " +
                    "que firma los tokens que este servicio valida; debe ser identica en ambos lados.\n" +
                    "En desarrollo configurala con:\n" +
                    "  dotnet user-secrets --project src/MRC.Agendia.Api set \"Jwt:Key\" \"<clave de Harmony>\"\n" +
                    "En produccion usa una variable de entorno Jwt__Key.");
            }
            // Validate the actual key size in bytes (HS256 needs >= 256 bits = 32 bytes),
            // not just the character count, so a short multi-byte key cannot slip through.
            if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Key es demasiado corta (minimo 32 bytes para HS256).");
            }
            return jwtKey;
        }
    }
}
