using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IClientRepository
    {
        Task<Client?> GetByIdAsync(int id);
        Task<IEnumerable<Client>> GetAllAsync();
        Task AddAsync(Client client);
        void Update(Client client);
        void Delete(Client client);
    }
}
