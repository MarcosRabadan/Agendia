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

        /// <inheritdoc />
        public Task<Business?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
            => Set
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        // IgnoreQueryFilters so availability works regardless of the caller's business
        // scope (#58); re-apply !IsDeleted explicitly since the global filter is bypassed.
        /// <inheritdoc />
        public Task<Business?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == id && b.IsActive && !b.IsDeleted, cancellationToken);
    }
}
