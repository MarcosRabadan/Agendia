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
    ///   <item><description>Empty list anywhere else: no cross-origin access at all.</description></item>
    /// </list>
    ///
    /// An empty list outside Development is the EXPECTED setup: Agendia is called
    /// backend-to-backend by Harmony, and a server-side caller is not subject to
    /// CORS. This used to fail-fast, back when a browser talked to this API
    /// directly. Origins are still honoured if configured, so exposing the service
    /// to a browser again only takes config, not a code change.
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
                        // No origins: the policy allows nothing cross-origin, which is
                        // what a backend-only service wants. Logged so an operator who
                        // DID expect browser traffic sees why it is being blocked.
                        Log.Information(
                            "CORS: sin origenes configurados en {Environment}. No se permite acceso cross-origin " +
                            "(Agendia se llama backend-to-backend desde Harmony). Si necesitas exponerla a un " +
                            "navegador, define Cors__AllowedOrigins__0, Cors__AllowedOrigins__1, ...",
                            environment.EnvironmentName);
                        return;
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
