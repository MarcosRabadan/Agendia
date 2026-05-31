using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AgendiaDbContext _context;

        public UnitOfWork(AgendiaDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<int> Save(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

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
