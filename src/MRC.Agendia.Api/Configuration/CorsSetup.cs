using Microsoft.Extensions.DependencyInjection;

namespace MRC.Agendia.Api.Configuration
{
    /// <summary>
    /// Politica CORS para que la app movil pueda llamar al API.
    ///
    /// En produccion debe restringirse a los dominios reales del front
    /// (issue #50 trata este punto).
    /// </summary>
    public static class CorsSetup
    {
        public const string DefaultPolicyName = "DefaultCors";

        public static IServiceCollection AddCorsForMobile(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod());
            });
            return services;
        }
    }
}
