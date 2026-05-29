using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IServiceRepository
    {
        Task<Service?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Service?> GetByIdPublicAsync(int id, CancellationToken cancellationToken = default);
        Task<Service?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<Service> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task AddAsync(Service service, CancellationToken cancellationToken = default);
        void Update(Service service);
        void Delete(Service service);
    }
}
