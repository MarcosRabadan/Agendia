using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IServiceRepository
    {
        Task<Service?> GetByIdAsync(int id);
        Task<IEnumerable<Service>> GetAllAsync();
        Task AddAsync(Service service);
        void Update(Service service);
        void Delete(Service service);
    }
}
