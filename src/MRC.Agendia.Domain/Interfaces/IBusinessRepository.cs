using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IBusinessRepository
    {
        Task<Business?> GetByIdAsync(int id);
        Task<IEnumerable<Business>> GetAllAsync();
        Task AddAsync(Business business);
        void Update(Business business);
        void Delete(Business business);
    }
}
