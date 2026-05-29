using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class ServiceRepository : RepositoryBase<Service>, IServiceRepository
    {
        public ServiceRepository(AgendiaDbContext context) : base(context)
        {
        }

        public Task<Service?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default)
            => Set
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        // Public (anonymous) catalog read by id: IgnoreQueryFilters so the service
        // detail / availability stays open regardless of the caller's business
        // scope (#58); re-apply !IsDeleted explicitly since the filter is bypassed.
        // The management paths (Update/Delete/validator) keep the scoped GetByIdAsync.
        public Task<Service?> GetByIdPublicAsync(int id, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        // Public (anonymous) catalog read: IgnoreQueryFilters so it stays a global
        // catalog regardless of the caller's business scope (#58); re-apply
        // !IsDeleted explicitly since the global filter is bypassed.
        public Task<(IReadOnlyList<Service> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);
    }
}
