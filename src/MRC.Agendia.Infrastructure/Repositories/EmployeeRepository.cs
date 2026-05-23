using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class EmployeeRepository : RepositoryBase<Employee>, IEmployeeRepository
    {
        public EmployeeRepository(AgendiaDbContext context) : base(context)
        {
        }

        public Task<Employee?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default)
            => Set
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        public Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .OrderBy(e => e.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedByOwnerUserIdAsync(string ownerUserId, int page, int pageSize, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .Where(e => e.Business.OwnerUserId == ownerUserId)
                .OrderBy(e => e.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public async Task<IEnumerable<Employee>> GetActiveByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
            => await Set.AsNoTracking()
                .Where(e => e.BusinessId == businessId && e.IsActive)
                .OrderBy(e => e.Id)
                .ToListAsync(cancellationToken);
    }
}
