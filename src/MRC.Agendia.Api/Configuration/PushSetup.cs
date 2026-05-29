using MRC.Agendia.Application.Common.Push;
using MRC.Agendia.Infrastructure.Push;
using Serilog;

namespace MRC.Agendia.Api.Configuration
{
    /// <summary>
    /// Registers the <see cref="IPushSender"/> implementation. Today only the
    /// logging sender exists, in every environment: the real provider (e.g. FCM via
    /// the Firebase Admin SDK) is deferred (issue #51) and would be selected here
    /// per environment with fail-fast on missing credentials, mirroring
    /// <see cref="EmailSetup"/>.
    /// </summary>
    public static class PushSetup
    {
        public static IServiceCollection AddPushSender(
            this IServiceCollection services,
            IHostEnvironment environment)
        {
            // No real push provider is wired yet: log-only everywhere. When one is
            // added, branch on the environment here (Logging in Dev/Testing, the real
            // provider otherwise) like EmailSetup does.
            Log.Warning(
                "Push: usando LoggingPushSender en {Environment}. No se envian push reales todavia (#51).",
                environment.EnvironmentName);
            services.AddScoped<IPushSender, LoggingPushSender>();
            return services;
        }
    }
}
