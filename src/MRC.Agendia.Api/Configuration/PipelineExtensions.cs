using System.Net;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using MRC.Agendia.Api.Middleware;

namespace MRC.Agendia.Api.Configuration
{
    /// <summary>
    /// Configura el HTTP request pipeline en el orden correcto.
    ///
    /// Orden critico:
    ///   1. ForwardedHeaders (solo fuera de Development/Testing)
    ///   2. HttpsRedirection
    ///   3. CORS
    ///   4. RateLimiter
    ///   5. ExceptionHandlingMiddleware
    ///   6. Authentication
    ///   7. Authorization
    ///   8. Controllers
    /// </summary>
    public static class PipelineExtensions
    {
        public static WebApplication UseConfiguredPipeline(this WebApplication app)
        {
            // ForwardedHeaders has to run BEFORE every other middleware so the
            // IP / scheme that the rate limiter, auth and logging see is the
            // real client one, not the proxy's. Disabled in Development (you
            // hit kestrel directly) and Testing (TestServer is not a proxy).
            if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
            {
                app.UseForwardedHeaders(BuildForwardedHeadersOptions(app.Configuration));
            }

            // Correlation id as early as possible so every log line and error
            // response (which echoes TraceIdentifier) carries the same id.
            app.UseMiddleware<CorrelationIdMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Agendia API v1");
                    c.RoutePrefix = string.Empty;
                });
            }

            // Skipped under "Testing" so WebApplicationFactory does not have to follow
            // 307 redirects when calling the API over TestServer.
            if (!app.Environment.IsEnvironment("Testing"))
            {
                app.UseHttpsRedirection();
            }

            app.UseCors();

            // Skipped under "Testing" because the auth rate limits (login 5/min,
            // register 3/h) collide with integration scenarios that exercise the
            // lockout-after-5-failures path or register several users in a row.
            if (!app.Environment.IsEnvironment("Testing"))
            {
                app.UseRateLimiter();
            }

            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            // /health      -> every check (full JSON report)
            // /health/ready-> critical dependencies (SQL, Seq) for orchestrators
            // /health/live -> process is up (no dependency checks)
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(HealthChecksSetup.ReadyTag),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });

            // Dashboard UI runs a background poller; not wired under Testing.
            if (!app.Environment.IsEnvironment("Testing"))
            {
                app.MapHealthChecksUI(options => options.UIPath = "/health-ui");
            }

            return app;
        }

        /// <summary>
        /// Builds the <see cref="ForwardedHeadersOptions"/> from configuration.
        /// By default ASP.NET only trusts loopback addresses; extra proxies and
        /// CIDR networks can be added via <c>ForwardedHeaders:KnownProxies</c>
        /// and <c>ForwardedHeaders:KnownNetworks</c>.
        /// </summary>
        private static ForwardedHeadersOptions BuildForwardedHeadersOptions(IConfiguration configuration)
        {
            var options = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                // Two hops cover the common topology: one internal LB + one edge proxy.
                // Bump if your infra chains more proxies in front of the API.
                ForwardLimit = 2,
            };

            var knownProxies = configuration
                .GetSection("ForwardedHeaders:KnownProxies")
                .Get<string[]>() ?? Array.Empty<string>();
            foreach (var raw in knownProxies)
            {
                if (IPAddress.TryParse(raw, out var ip))
                {
                    options.KnownProxies.Add(ip);
                }
            }

            var knownNetworks = configuration
                .GetSection("ForwardedHeaders:KnownNetworks")
                .Get<string[]>() ?? Array.Empty<string>();
            foreach (var raw in knownNetworks)
            {
                // Expected format: "ip/prefix" e.g. "10.0.0.0/8".
                var parts = raw.Split('/');
                if (parts.Length == 2
                    && IPAddress.TryParse(parts[0], out var netIp)
                    && int.TryParse(parts[1], out var prefix))
                {
                    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(netIp, prefix));
                }
            }

            return options;
        }
    }
}
