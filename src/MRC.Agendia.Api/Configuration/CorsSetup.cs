using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace MRC.Agendia.Api.Configuration
{
    /// <summary>
    /// Politica CORS configurable por entorno. Lee los origenes permitidos
    /// desde la seccion <c>Cors:AllowedOrigins</c> de configuracion.
    ///
    /// Comportamiento segun entorno:
    /// <list type="bullet">
    ///   <item><description>Origenes definidos: politica restringida con esos hosts.</description></item>
    ///   <item><description>Lista vacia en Development: fallback permisivo (AllowAnyOrigin) con warning.</description></item>
    ///   <item><description>Lista vacia fuera de Development: fail-fast (no arranca).</description></item>
    /// </list>
    /// </summary>
    public static class CorsSetup
    {
        public const string DefaultPolicyName = "DefaultCors";

        public static IServiceCollection AddCorsForMobile(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            var allowedOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? Array.Empty<string>();

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                        return;
                    }

                    if (!environment.IsDevelopment())
                    {
                        throw new InvalidOperationException(
                            "Cors:AllowedOrigins esta vacio en un entorno distinto de Development. " +
                            "Configura la lista de origenes permitidos via variable de entorno " +
                            "Cors__AllowedOrigins__0, Cors__AllowedOrigins__1, ... o en appsettings.<Env>.json.");
                    }

                    // Development fallback: permissive policy with a clear warning so
                    // the dev notices the missing config but the app keeps running.
                    Log.Warning(
                        "CORS: Cors:AllowedOrigins esta vacio en Development. " +
                        "Aplicando AllowAnyOrigin como fallback. Define los origenes en appsettings.Development.json.");

                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            return services;
        }
    }
}
