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
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.WithProcessId()
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();

            builder.Host.UseSerilog();
            return builder;
        }
    }
}
