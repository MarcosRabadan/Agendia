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

        public Task<(IReadOnlyList<Service> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .OrderBy(s => s.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);
    }
}
