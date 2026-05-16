using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<Employee?> GetByIdAsync(int id);
        Task<IEnumerable<Employee>> GetAllAsync();
        Task<IEnumerable<Employee>> GetByBusinessIdAsync(int businessId, bool onlyActive = true);
        Task AddAsync(Employee employee);
        void Update(Employee employee);
        void Delete(Employee employee);
    }
}
