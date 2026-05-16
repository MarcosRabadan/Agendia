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
    /// Configuracion de ASP.NET Identity + JWT Bearer.
    ///
    /// Identity se configura con politicas de password razonables y lockout
    /// (5 intentos -> bloqueo de 15 minutos). El esquema JWT lee la clave
    /// desde configuracion (Jwt:Key) y valida issuer, audience y firma.
    /// </summary>
    public static class AuthenticationSetup
    {
        public static IServiceCollection AddIdentityAndJwt(this IServiceCollection services, IConfiguration configuration)
        {
            // Identity
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
            })
            .AddEntityFrameworkStores<AgendiaDbContext>()
            .AddDefaultTokenProviders();

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
                // Se configura desde Program.cs via post-config si hace falta variar por entorno
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
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

            services.AddAuthorization();

            // Resource-based authorization helpers (necesita HttpContext)
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserContext, CurrentUserContext>();

            return services;
        }

        /// <summary>Validacion fail-fast: la app NO arranca si Jwt:Key no esta bien configurada.</summary>
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
            if (jwtKey.Length < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Key es demasiado corta (minimo 32 caracteres recomendado para HS256).");
            }
            return jwtKey;
        }
    }
}
