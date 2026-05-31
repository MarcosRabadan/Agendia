namespace MRC.Agendia.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>Persists all pending changes tracked by the context.</summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The number of state entries written to the database.</returns>
        Task<int> Save(CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs <paramref name="work"/> inside a single database transaction: it
        /// commits on success and rolls back on exception. Use it to make a
        /// multi-step unit of work atomic - e.g. a delete-then-insert that must not
        /// apply partially. On a non-relational provider (EF InMemory in tests) the
        /// work runs directly, with no transaction.
        /// </summary>
        /// <param name="work">The work to run transactionally (typically issues repository calls and one or more Save).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task ExecuteInTransactionAsync(Func<Task> work, CancellationToken cancellationToken = default);
    }
}
