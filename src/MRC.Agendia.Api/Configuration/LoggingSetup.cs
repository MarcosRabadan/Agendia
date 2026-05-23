using Serilog;

namespace MRC.Agendia.Api.Configuration
{
    /// <summary>
    /// Serilog configuration (console + Seq sink).
    /// </summary>
    public static class LoggingSetup
    {
        public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder)
        {
            // Same Seq URL used by the health checks; falls back to the local
            // default so a dev machine works out of the box.
            var seqUrl = builder.Configuration["HealthChecks:SeqUrl"] ?? "http://localhost:5341";

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.WithProcessId()
                .WriteTo.Console()
                .WriteTo.Seq(seqUrl)
                .CreateLogger();

            builder.Host.UseSerilog();
            return builder;
        }
    }
}
