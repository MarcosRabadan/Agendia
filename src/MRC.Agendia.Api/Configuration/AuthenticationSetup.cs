using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Api.Services;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Identity;

namespace MRC.Agendia.Api.Configuration
{
    /// <summary>
    /// Configures ASP.NET Identity + JWT Bearer authentication.
    ///
    /// Identity is configured with reasonable password policies and lockout
    /// (5 failed attempts -> 15-minute block). The JWT scheme reads the key
    /// from configuration (Jwt:Key) and validates issuer, audience and signature.
    /// </summary>
    public static class AuthenticationSetup
    {
        // Name of the short-lived token provider used for password reset.
        private const string PasswordResetProvider = "PasswordResetShortLived";

        public static IServiceCollection AddIdentityAndJwt(this IServiceCollection services, IConfiguration configuration)
        {
            // Identity password and lockout policy.
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;

                options.User.RequireUniqueEmail = true;

                // Password reset uses a dedicated short-lived provider; email
                // confirmation keeps the default DataProtectorTokenProvider.
                options.Tokens.PasswordResetTokenProvider = PasswordResetProvider;
            })
            .AddEntityFrameworkStores<AgendiaDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<ShortLivedTokenProvider<ApplicationUser>>(PasswordResetProvider);

            // Token lifespans (configurable). Email confirmation rides on the
            // default provider; password reset on the short-lived one.
            var emailConfirmHours = configuration.GetValue<int?>("Auth:EmailConfirmationTokenHours") ?? 24;
            var resetHours = configuration.GetValue<int?>("Auth:PasswordResetTokenHours") ?? 1;
            services.Configure<DataProtectionTokenProviderOptions>(o =>
                o.TokenLifespan = TimeSpan.FromHours(emailConfirmHours));
            services.Configure<ShortLivedTokenProviderOptions>(o =>
                o.TokenLifespan = TimeSpan.FromHours(resetHours));

            // JWT
            var jwtKey = ValidateAndGetJwtKey(configuration);
            var jwtSection = configuration.GetSection("Jwt");
            var jwtIssuer = jwtSection["Issuer"];
            var jwtAudience = jwtSection["Audience"];

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                // Per-environment overrides can be applied from Program.cs via post-configure if needed.
                options.RequireHttpsMetadata = true;
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
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

            services.AddAuthorization();

            // Resource-based authorization helpers (need HttpContext to read the caller).
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserContext, CurrentUserContext>();

            return services;
        }

        /// <summary>Fail-fast validation: the app does NOT start if Jwt:Key is missing or too short.</summary>
        private static string ValidateAndGetJwtKey(IConfiguration configuration)
        {
            var jwtKey = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException(
                    "Jwt:Key no esta configurado. En desarrollo configuralo con:\n" +
                    "  dotnet user-secrets --project src/MRC.Agendia.Api set \"Jwt:Key\" \"<clave aleatoria>\"\n" +
                    "En produccion usa una variable de entorno Jwt__Key.\n" +
                    "Genera una clave fuerte (>=64 chars) con: openssl rand -base64 64");
            }
            // Validate the actual key size in bytes (HS256 needs >= 256 bits = 32 bytes),
            // not just the character count, so a short multi-byte key cannot slip through.
            if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Key es demasiado corta (minimo 32 bytes para HS256; genera >=64 con: openssl rand -base64 64).");
            }
            return jwtKey;
        }
    }
}
