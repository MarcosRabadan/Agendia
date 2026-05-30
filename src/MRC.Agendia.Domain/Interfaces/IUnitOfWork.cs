namespace MRC.Agendia.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>Persists all pending changes tracked by the context.</summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The number of state entries written to the database.</returns>
        Task<int> Save(CancellationToken cancellationToken = default);
    }
}
