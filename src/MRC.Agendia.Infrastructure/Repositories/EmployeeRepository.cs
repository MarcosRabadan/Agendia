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

        public async Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => await _context.Employees.FindAsync(new object?[] { id }, cancellationToken);

        public Task<Employee?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default)
            => _context.Employees
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        public async Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
            => await _context.Employees.ToListAsync(cancellationToken);

        public Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => _context.Employees
                .AsNoTracking()
                .OrderBy(e => e.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedByOwnerUserIdAsync(string ownerUserId, int page, int pageSize, CancellationToken cancellationToken = default)
            => _context.Employees
                .AsNoTracking()
                .Where(e => e.Business.OwnerUserId == ownerUserId)
                .OrderBy(e => e.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public async Task<IEnumerable<Employee>> GetByBusinessIdAsync(int businessId, bool onlyActive = true, CancellationToken cancellationToken = default)
        {
            var query = _context.Employees.AsNoTracking()
                .Where(e => e.BusinessId == businessId);

            if (onlyActive)
                query = query.Where(e => e.IsActive);

            return await query.OrderBy(e => e.Id).ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
            => await _context.Employees.AddAsync(employee, cancellationToken);

        public void Update(Employee employee)
            => _context.Employees.Update(employee);

        public void Delete(Employee employee)
            => _context.Employees.Remove(employee);
    }
}
