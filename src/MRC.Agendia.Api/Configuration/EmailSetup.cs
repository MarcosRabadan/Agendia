using MRC.Agendia.Application.Common.Email;
using MRC.Agendia.Infrastructure.Email;
using Serilog;

namespace MRC.Agendia.Api.Configuration
{
    /// <summary>
    /// Registers the <see cref="IEmailSender"/> implementation per environment.
    ///
    /// Behavior per environment:
    /// <list type="bullet">
    ///   <item><description>Development / Testing: <see cref="LoggingEmailSender"/> (writes the link to the log, no SMTP needed).</description></item>
    ///   <item><description>Any other environment: <see cref="SmtpEmailSender"/>, fail-fast if <c>Email:Smtp:Host</c> or <c>Email:Smtp:From</c> are missing.</description></item>
    /// </list>
    /// </summary>
    public static class EmailSetup
    {
        public static IServiceCollection AddEmailSender(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            var isNonProdEnv = environment.IsDevelopment()
                || environment.IsEnvironment("Testing");

            if (isNonProdEnv)
            {
                Log.Warning(
                    "Email: usando LoggingEmailSender en {Environment}. " +
                    "Los enlaces de reset/confirmacion se escriben en el log, no se envia correo real.",
                    environment.EnvironmentName);
                services.AddScoped<IEmailSender, LoggingEmailSender>();
                return services;
            }

            var host = configuration["Email:Smtp:Host"];
            var from = configuration["Email:Smtp:From"];
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            {
                throw new InvalidOperationException(
                    "Email:Smtp:Host y Email:Smtp:From son obligatorios fuera de Development/Testing. " +
                    "Configuralos via variables de entorno Email__Smtp__Host, Email__Smtp__From, " +
                    "Email__Smtp__User, Email__Smtp__Password, Email__Smtp__Port, Email__Smtp__EnableSsl.");
            }

            // User and Password must be set together. A half-configured credential (one
            // without the other) would silently fall back to an anonymous send that fails
            // at delivery time (and is swallowed for best-effort emails). Both empty is a
            // valid intentional anonymous relay; warn so it is a conscious choice.
            var hasUser = !string.IsNullOrWhiteSpace(configuration["Email:Smtp:User"]);
            var hasPassword = !string.IsNullOrWhiteSpace(configuration["Email:Smtp:Password"]);
            if (hasUser != hasPassword)
            {
                throw new InvalidOperationException(
                    "Email:Smtp:User y Email:Smtp:Password deben configurarse juntos, o ninguno para un relay anonimo. " +
                    "Configura ambos via Email__Smtp__User / Email__Smtp__Password.");
            }
            if (!hasUser)
            {
                Log.Warning(
                    "Email: SMTP sin credenciales (Email:Smtp:User vacio) -> envio anonimo contra {Host}. " +
                    "Asegurate de que el relay acepta envio sin autenticacion.", host);
            }

            services.AddScoped<IEmailSender, SmtpEmailSender>();
            return services;
        }
    }
}
