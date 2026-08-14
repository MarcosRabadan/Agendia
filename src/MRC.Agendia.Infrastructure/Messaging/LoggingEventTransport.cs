using Microsoft.Extensions.Logging;

namespace MRC.Agendia.Infrastructure.Messaging
{
    /// <summary>
    /// Placeholder <see cref="IEventTransport"/> that logs the event instead of
    /// delivering it to a broker. Keeps Agendia decoupled from the transport until
    /// the system-wide broker is chosen; swap this registration for the real
    /// adapter then (see <c>DependencyInjection</c>).
    /// </summary>
    public class LoggingEventTransport : IEventTransport
    {
        private readonly ILogger<LoggingEventTransport> _logger;

        public LoggingEventTransport(ILogger<LoggingEventTransport> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task PublishAsync(string type, string payload, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Evento de integracion {Type} publicado (transporte log-only): {Payload}", type, payload);
            return Task.CompletedTask;
        }
    }
}
