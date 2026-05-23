using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MRC.Agendia.Api.Configuration
{
    /// <summary>
    /// Rich health checks for deployment/orchestration:
    /// <list type="bullet">
    ///   <item><description>SQL Server connectivity (critical, tagged "ready").</description></item>
    ///   <item><description>Seq reachability (non-critical: reports Degraded, never fails readiness).</description></item>
    /// </list>
    /// Endpoints are mapped in <see cref="PipelineExtensions"/>:
    /// <c>/health</c> (all), <c>/health/ready</c> (deps), <c>/health/live</c> (process), <c>/health-ui</c> (dashboard).
    /// </summary>
    public static class HealthChecksSetup
    {
        public const string ReadyTag = "ready";

        public static IServiceCollection AddAppHealthChecks(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            var seqUrl = configuration["HealthChecks:SeqUrl"] ?? "http://localhost:5341";

            services.AddHealthChecks()
                .AddSqlServer(
                    connectionString,
                    name: "sql-server",
                    tags: new[] { ReadyTag })
                .AddUrlGroup(
                    new Uri(seqUrl),
                    name: "seq",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { ReadyTag });

            // The dashboard UI spins up a background poller; skip it under Testing
            // so the integration host (TestServer) does not start polling.
            if (!environment.IsEnvironment("Testing"))
            {
                services
                    .AddHealthChecksUI(setup =>
                    {
                        setup.AddHealthCheckEndpoint("Agendia API", "/health/ready");
                        setup.SetEvaluationTimeInSeconds(30);
                    })
                    .AddInMemoryStorage();
            }

            return services;
        }
    }
}
