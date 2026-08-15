using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MRC.Agendia.Infrastructure.Messaging
{
    /// <summary>
    /// Polls the outbox and delivers pending events through <see cref="IEventTransport"/>
    /// (at-least-once), then periodically purges old processed rows. The per-cycle work
    /// lives in <see cref="OutboxProcessor"/>; this service only owns the timing loop.
    ///
    /// A message is marked processed only after a successful delivery, so a crash or broker
    /// outage retries it next poll instead of losing it. A permanently-failing message is
    /// dead-lettered after <see cref="OutboxOptions.MaxAttempts"/> so it stops blocking the
    /// queue (see <see cref="OutboxProcessor.DispatchPendingAsync"/>).
    ///
    /// <para><b>Single instance.</b> The poll does not claim rows (no <c>FOR UPDATE SKIP
    /// LOCKED</c>), so running more than one instance would deliver each event from every
    /// instance. That is tolerable under at-least-once with an idempotent consumer, but it
    /// amplifies duplicates, so run ONE instance until a claim (or the real broker) is wired.</para>
    ///
    /// Configuration (optional, safe defaults): see <see cref="OutboxOptions"/> ("Outbox"
    /// section: PollIntervalSeconds, BatchSize, MaxAttempts, RetentionDays, PurgeIntervalMinutes).
    /// </summary>
    public class OutboxDispatcherService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly OutboxOptions _options;
        private readonly ILogger<OutboxDispatcherService> _logger;
        private DateTime _nextPurgeUtc = DateTime.MinValue;

        public OutboxDispatcherService(IServiceProvider serviceProvider,
                                       IOptions<OutboxOptions> options,
                                       ILogger<OutboxDispatcherService> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds));
            _logger.LogInformation(
                "OutboxDispatcherService started (single instance). Interval: {Interval}, batch: {BatchSize}, maxAttempts: {MaxAttempts}, retention: {RetentionDays}d.",
                interval, _options.BatchSize, _options.MaxAttempts, _options.RetentionDays);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox dispatch cycle failed; retrying in {Interval}.", interval);
                }

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task RunCycleAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();

            var delivered = await processor.DispatchPendingAsync(cancellationToken);
            if (delivered > 0)
                _logger.LogInformation("Dispatched {Delivered} outbox event(s).", delivered);

            // Purge runs on its own (longer) cadence, not every poll, so it never competes
            // with delivery latency.
            if (DateTime.UtcNow >= _nextPurgeUtc)
            {
                var purged = await processor.PurgeProcessedAsync(cancellationToken);
                if (purged > 0)
                    _logger.LogInformation("Purged {Purged} processed outbox message(s) older than {RetentionDays} day(s).",
                        purged, _options.RetentionDays);

                _nextPurgeUtc = DateTime.UtcNow.AddMinutes(Math.Max(1, _options.PurgeIntervalMinutes));
            }
        }
    }
}
