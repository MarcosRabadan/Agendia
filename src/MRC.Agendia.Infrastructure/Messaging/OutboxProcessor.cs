using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MRC.Agendia.Infrastructure.Messaging
{
    /// <summary>
    /// The work the outbox dispatcher runs each cycle: deliver a batch of pending events
    /// through <see cref="IEventTransport"/> and purge old processed rows. Extracted from
    /// the <see cref="OutboxDispatcherService"/> BackgroundService (which only handles the
    /// timing loop) so this logic is unit-testable with an in-memory context.
    /// </summary>
    public class OutboxProcessor
    {
        private readonly AgendiaDbContext _context;
        private readonly IEventTransport _transport;
        private readonly OutboxOptions _options;
        private readonly ILogger<OutboxProcessor> _logger;

        public OutboxProcessor(AgendiaDbContext context,
                               IEventTransport transport,
                               IOptions<OutboxOptions> options,
                               ILogger<OutboxProcessor> logger)
        {
            _context = context;
            _transport = transport;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Delivers the oldest pending events (at-least-once). A message is marked processed
        /// only after a successful delivery; a failure leaves it pending for the next cycle,
        /// so a single failing message never stops the rest of the batch. Messages that have
        /// exhausted <see cref="OutboxOptions.MaxAttempts"/> are excluded from the poll, so a
        /// permanently-failing ("poison") message can no longer sit at the head of the queue
        /// and starve newer ones - it is dead-lettered in place (kept with its Error for
        /// inspection/replay). Returns the number delivered.
        /// </summary>
        public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken = default)
        {
            var pending = await _context.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc == null && m.Attempts < _options.MaxAttempts)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);

            if (pending.Count == 0)
                return 0;

            var delivered = 0;
            foreach (var message in pending)
            {
                message.Attempts++;
                try
                {
                    await _transport.PublishAsync(message.Type, message.Payload, cancellationToken);
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    message.Error = null;
                    delivered++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Leave it pending (ProcessedOnUtc stays null) so the next cycle retries it.
                    message.Error = ex.Message;
                    if (message.Attempts >= _options.MaxAttempts)
                        _logger.LogError(ex,
                            "Outbox message {Id} ({Type}) dead-lettered after {Attempts} failed attempts; it is excluded from further delivery and kept for inspection.",
                            message.Id, message.Type, message.Attempts);
                    else
                        _logger.LogWarning(ex,
                            "Failed to dispatch outbox message {Id} ({Type}); will retry (attempt {Attempts}/{Max}).",
                            message.Id, message.Type, message.Attempts, _options.MaxAttempts);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return delivered;
        }

        /// <summary>
        /// Deletes processed messages older than <see cref="OutboxOptions.RetentionDays"/> so
        /// the outbox table stays bounded. Returns the number removed.
        /// </summary>
        public async Task<int> PurgeProcessedAsync(CancellationToken cancellationToken = default)
        {
            var cutoff = DateTime.UtcNow.AddDays(-_options.RetentionDays);
            var stale = _context.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc != null && m.ProcessedOnUtc < cutoff);

            // ExecuteDelete is a single efficient DELETE on a real database; the in-memory
            // provider used in tests does not support it, so fall back to a tracked delete.
            if (_context.Database.IsRelational())
                return await stale.ExecuteDeleteAsync(cancellationToken);

            var rows = await stale.ToListAsync(cancellationToken);
            if (rows.Count == 0)
                return 0;

            _context.Set<OutboxMessage>().RemoveRange(rows);
            await _context.SaveChangesAsync(cancellationToken);
            return rows.Count;
        }
    }
}
