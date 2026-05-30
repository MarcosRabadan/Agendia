using Serilog;

namespace MRC.Agendia.Api.Configuration
{
    /// <summary>
    /// Per-environment CORS policy. Reads the allowed origins from the
    /// <c>Cors:AllowedOrigins</c> configuration section.
    ///
    /// Behavior per environment:
    /// <list type="bullet">
    ///   <item><description>Origins defined: restricted policy bound to those hosts.</description></item>
    ///   <item><description>Empty list in Development or Testing: permissive fallback (AllowAnyOrigin) with warning.</description></item>
    ///   <item><description>Empty list in any other environment (Production, Staging, ...): fail-fast (app does not start).</description></item>
    /// </list>
    /// </summary>
    public static class CorsSetup
    {
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

                    var isNonProdEnv = environment.IsDevelopment()
                        || environment.IsEnvironment("Testing");

                    if (!isNonProdEnv)
                    {
                        throw new InvalidOperationException(
                            "Cors:AllowedOrigins esta vacio en un entorno distinto de Development/Testing. " +
                            "Configura la lista de origenes permitidos via variable de entorno " +
                            "Cors__AllowedOrigins__0, Cors__AllowedOrigins__1, ... o en appsettings.<Env>.json.");
                    }

                    // Development / Testing fallback: permissive policy with a clear warning so
                    // the dev notices the missing config but the app keeps running.
                    Log.Warning(
                        "CORS: Cors:AllowedOrigins esta vacio en {Environment}. " +
                        "Aplicando AllowAnyOrigin como fallback.",
                        environment.EnvironmentName);

                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            return services;
        }
    }
}
