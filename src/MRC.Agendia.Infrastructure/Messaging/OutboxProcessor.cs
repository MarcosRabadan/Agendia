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
        /// inspection/replay).
        ///
        /// <para><b>N-instance safe.</b> On a real database the batch is claimed with
        /// <c>FOR UPDATE SKIP LOCKED</c> inside a transaction: each running instance locks
        /// its own rows and the others skip those locked rows, so every event is delivered
        /// exactly once per commit no matter how many replicas run this dispatcher. The
        /// in-memory provider (unit tests) has no shared database to race against and supports
        /// neither raw SQL nor row locks, so it takes a plain poll.</para>
        ///
        /// Returns the number delivered.
        /// </summary>
        public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken = default)
        {
            // In-memory provider (unit tests): no shared DB, no raw SQL, no row locks.
            if (!_context.Database.IsRelational())
            {
                var pending = await QueryPending().ToListAsync(cancellationToken);
                if (pending.Count == 0)
                    return 0;

                var count = await DeliverAsync(pending, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                return count;
            }

            // Relational: claim the batch with FOR UPDATE SKIP LOCKED inside a transaction so
            // concurrent instances never pick the same rows. FromSql is NOT composed with LINQ
            // on purpose - composing would wrap it in a subquery and the FOR UPDATE would be
            // lost. The lock is held only for these rows and only until the transaction commits.
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var claimed = await _context.Set<OutboxMessage>()
                .FromSqlInterpolated($@"
                    SELECT * FROM ""OutboxMessages""
                    WHERE ""ProcessedOnUtc"" IS NULL AND ""Attempts"" < {_options.MaxAttempts}
                    ORDER BY ""OccurredOnUtc""
                    LIMIT {_options.BatchSize}
                    FOR UPDATE SKIP LOCKED")
                .ToListAsync(cancellationToken);

            if (claimed.Count == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return 0;
            }

            var delivered = await DeliverAsync(claimed, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return delivered;
        }

        private IQueryable<OutboxMessage> QueryPending()
            => _context.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc == null && m.Attempts < _options.MaxAttempts)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(_options.BatchSize);

        // Delivers each claimed message through the transport, marking it processed on success
        // or recording the failure (and dead-lettering once it exhausts MaxAttempts). Does NOT
        // save - the caller persists (inside its transaction on the relational path). Returns
        // the number delivered.
        private async Task<int> DeliverAsync(IReadOnlyList<OutboxMessage> messages, CancellationToken cancellationToken)
        {
            var delivered = 0;
            foreach (var message in messages)
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
