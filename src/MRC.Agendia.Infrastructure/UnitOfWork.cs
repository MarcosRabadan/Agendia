using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Infrastructure.Caching;
using Npgsql;

namespace MRC.Agendia.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        // Filtered unique index that dedups active ("Waiting") waitlist entries per slot
        // (see AgendiaDbContext). Npgsql reports it as the violated constraint name.
        private const string WaitlistUniqueWaitingIndexName = "IX_WaitlistEntry_UniqueWaiting";

        // Filtered unique index that keeps a business to one default template (#307).
        private const string ScheduleTemplateOneDefaultIndexName = "IX_ScheduleTemplate_OneDefaultPerBusiness";

        private readonly AgendiaDbContext _context;
        private readonly PendingCacheInvalidations _pendingInvalidations;

        // True while ExecuteInTransactionAsync is running, so the saves inside it hold their
        // cache evictions back until the transaction actually commits (#306).
        private bool _inTransaction;

        public UnitOfWork(AgendiaDbContext context, PendingCacheInvalidations pendingInvalidations)
        {
            _context = context;
            _pendingInvalidations = pendingInvalidations;
        }

        /// <inheritdoc />
        public async Task<int> Save(CancellationToken cancellationToken = default)
        {
            try
            {
                var affected = await _context.SaveChangesAsync(cancellationToken);

                // Evict only once the write is durable (#306). Inside a transaction the rows
                // are not visible to anyone else yet, so the eviction waits for the commit -
                // otherwise a concurrent reader would re-cache the pre-write state.
                if (!_inTransaction)
                    _pendingInvalidations.Flush();

                return affected;
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
            {
                // A check-then-insert race that slipped past the app-level pre-check and hit
                // a unique index surfaces here as a raw DbUpdateException (-> 500). Translate
                // the known constraint into its typed domain exception so the API returns a
                // clean 4xx (as the pre-check already does in the non-racing path). Unknown
                // unique violations keep their original behaviour (rethrown as-is).
                if (pg.ConstraintName == WaitlistUniqueWaitingIndexName)
                    throw new DuplicateWaitlistEntryException();
                if (pg.ConstraintName == ScheduleTemplateOneDefaultIndexName)
                    throw new DuplicateDefaultScheduleTemplateException();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task ExecuteInTransactionAsync(Func<Task> work, CancellationToken cancellationToken = default)
        {
            // EF InMemory (tests) does not support transactions, so run directly there
            // - mirrors the IsRelational guard used elsewhere. No EF retrying execution
            // strategy is enabled, so a manual transaction is safe.
            if (!_context.Database.IsRelational())
            {
                await work();
                return;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            _inTransaction = true;
            try
            {
                await work();
                await transaction.CommitAsync(cancellationToken);
            }
            finally
            {
                _inTransaction = false;
            }

            // Committed: now the evictions the inner saves queued up can happen. On a rollback
            // we never get here, and the queued keys simply cost a later cache miss.
            _pendingInvalidations.Flush();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

    }
}
