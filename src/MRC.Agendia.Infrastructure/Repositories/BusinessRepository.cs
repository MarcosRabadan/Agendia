using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class BusinessRepository : RepositoryBase<Business>, IBusinessRepository
    {
        public BusinessRepository(AgendiaDbContext context) : base(context)
        {
        }

        public Task<Business?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default)
            => Set
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        // Public (anonymous) reads: IgnoreQueryFilters so the catalog stays open to
        // everyone regardless of the caller's business scope (#58); re-apply
        // !IsDeleted explicitly since the global filter is bypassed.
        public Task<Business?> GetActiveByIdAsync(int id, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == id && b.IsActive && !b.IsDeleted, cancellationToken);

        public Task<(IReadOnlyList<Business> Items, int TotalCount)> GetPagedActiveAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(b => b.IsActive && !b.IsDeleted)
                .OrderBy(b => b.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);
    }
}
