using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IClientRepository
    {
        Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Client?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default);
        Task<Client?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task AddAsync(Client client, CancellationToken cancellationToken = default);
        void Update(Client client);
        void Delete(Client client);
    }
}
