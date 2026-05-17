using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IClientRepository
    {
        Task<Client?> GetByIdAsync(int id);
        Task<Client?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Client>> GetAllAsync();
        Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task AddAsync(Client client);
        void Update(Client client);
        void Delete(Client client);
    }
}
