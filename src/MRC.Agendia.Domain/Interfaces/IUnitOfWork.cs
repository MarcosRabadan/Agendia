namespace MRC.Agendia.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> Save(CancellationToken cancellationToken = default);
    }
}
