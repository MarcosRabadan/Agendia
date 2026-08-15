using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;
using Npgsql;

namespace MRC.Agendia.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        // Filtered unique index that dedups active ("Waiting") waitlist entries per slot
        // (see AgendiaDbContext). Npgsql reports it as the violated constraint name.
        private const string WaitlistUniqueWaitingIndexName = "IX_WaitlistEntry_UniqueWaiting";

        private readonly AgendiaDbContext _context;

        public UnitOfWork(AgendiaDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<int> Save(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
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
            await work();
            await transaction.CommitAsync(cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

    }
}
