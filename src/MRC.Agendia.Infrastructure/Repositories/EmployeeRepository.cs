using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly AgendiaDbContext _context;

        public EmployeeRepository(AgendiaDbContext context)
        {
            _context = context;
        }

        public async Task<Employee?> GetByIdAsync(int id)
            => await _context.Employees.FindAsync(id);

        public async Task<IEnumerable<Employee>> GetAllAsync()
            => await _context.Employees.ToListAsync();

        public Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => _context.Employees
                .AsNoTracking()
                .OrderBy(e => e.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public async Task<IEnumerable<Employee>> GetByBusinessIdAsync(int businessId, bool onlyActive = true)
        {
            var query = _context.Employees.AsNoTracking()
                .Where(e => e.BusinessId == businessId);

            if (onlyActive)
                query = query.Where(e => e.IsActive);

            return await query.OrderBy(e => e.Id).ToListAsync();
        }

        public async Task AddAsync(Employee employee)
            => await _context.Employees.AddAsync(employee);

        public void Update(Employee employee)
            => _context.Employees.Update(employee);

        public void Delete(Employee employee)
            => _context.Employees.Remove(employee);
    }
}
