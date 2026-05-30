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

        /// <inheritdoc />
        public Task<Employee?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default)
            => Set
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        // Public (anonymous) read by id for the booking/availability flow:
        // IgnoreQueryFilters so it works regardless of the caller's business scope
        // (#58); re-apply !IsDeleted since the filter is bypassed. The caller still
        // checks BusinessId/IsActive. Management paths keep the scoped GetByIdAsync.
        /// <inheritdoc />
        public Task<Employee?> GetByIdPublicAsync(int id, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

        /// <inheritdoc />
        public Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .OrderBy(e => e.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        /// <inheritdoc />
        public Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedByOwnerUserIdAsync(
            string ownerUserId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .Where(e => e.Business.OwnerUserId == ownerUserId)
                .OrderBy(e => e.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        // Used only by the public availability flow: IgnoreQueryFilters so an
        // authenticated owner/employee can read another business's active staff
        // (#58); re-apply !IsDeleted since the global business filter is bypassed.
        /// <inheritdoc />
        public async Task<IEnumerable<Employee>> GetActiveByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
            => await Set.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(e => e.BusinessId == businessId && e.IsActive && !e.IsDeleted)
                .OrderBy(e => e.Id)
                .ToListAsync(cancellationToken);
    }
}
