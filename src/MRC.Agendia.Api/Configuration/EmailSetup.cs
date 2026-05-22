using Microsoft.Extensions.DependencyInjection;
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

            services.AddScoped<IEmailSender, SmtpEmailSender>();
            return services;
        }
    }
}
