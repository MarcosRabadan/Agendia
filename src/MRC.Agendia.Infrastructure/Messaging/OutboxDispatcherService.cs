using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MRC.Agendia.Infrastructure.Messaging
{
    /// <summary>
    /// Polls the outbox and delivers pending events through <see cref="IEventTransport"/>
    /// (at-least-once). A message is marked processed only after a successful
    /// delivery, so a crash or broker outage retries it on the next poll instead of
    /// losing it. A failed delivery does not block the rest of the batch.
    ///
    /// Configuration (optional, with safe defaults):
    ///   "Outbox": {
    ///     "PollIntervalSeconds": 10,
    ///     "BatchSize": 20
    ///   }
    /// </summary>
    public class OutboxDispatcherService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxDispatcherService> _logger;
        private readonly TimeSpan _interval;
        private readonly int _batchSize;

        public OutboxDispatcherService(IServiceProvider serviceProvider,
                                       IConfiguration configuration,
                                       ILogger<OutboxDispatcherService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            var section = configuration.GetSection("Outbox");
            var intervalSeconds = section.GetValue<int?>("PollIntervalSeconds") ?? 10;
            var batchSize = section.GetValue<int?>("BatchSize") ?? 20;

            _interval = TimeSpan.FromSeconds(Math.Max(1, intervalSeconds));
            _batchSize = Math.Max(1, batchSize);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "OutboxDispatcherService iniciado. Intervalo: {Interval}, Lote: {BatchSize}.",
                _interval, _batchSize);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DispatchPendingAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error despachando eventos del outbox. Se reintentara en {Interval}.", _interval);
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task DispatchPendingAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var transport = scope.ServiceProvider.GetRequiredService<IEventTransport>();

            var pending = await context.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc == null)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(_batchSize)
                .ToListAsync(cancellationToken);

            if (pending.Count == 0)
                return;

            var delivered = 0;
            foreach (var message in pending)
            {
                message.Attempts++;
                try
                {
                    await transport.PublishAsync(message.Type, message.Payload, cancellationToken);
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    message.Error = null;
                    delivered++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Leave it pending (ProcessedOnUtc stays null) so it is retried
                    // next poll; a single poison message does not stop the batch.
                    message.Error = ex.Message;
                    _logger.LogWarning(ex, "Fallo al despachar el evento {Id} ({Type}); se reintentara.", message.Id, message.Type);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Despachados {Delivered} de {Total} evento(s) del outbox.", delivered, pending.Count);
        }
    }
}
