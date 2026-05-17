using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IBusinessRepository
    {
        Task<Business?> GetByIdAsync(int id);
        Task<Business?> GetActiveByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Business>> GetAllAsync();
        Task<(IReadOnlyList<Business> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<Business> Items, int TotalCount)> GetPagedActiveAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task AddAsync(Business business);
        void Update(Business business);
        void Delete(Business business);
    }
}
