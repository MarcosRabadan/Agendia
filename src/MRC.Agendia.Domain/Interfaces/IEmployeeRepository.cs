using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Employee?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Employee>> GetActiveByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedByOwnerUserIdAsync(string ownerUserId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
        void Update(Employee employee);
        void Delete(Employee employee);
    }
}
