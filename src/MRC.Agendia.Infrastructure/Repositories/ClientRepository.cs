using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class ClientRepository : RepositoryBase<Client>, IClientRepository
    {
        public ClientRepository(AgendiaDbContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public Task<Client?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default)
            => Set
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        /// <inheritdoc />
        public Task<Client?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        /// <inheritdoc />
        public Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        /// <inheritdoc />
        public Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedByBusinessIdAsync(int businessId,
                                                                                             int page,
                                                                                             int pageSize,
                                                                                             CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .Where(c => c.BusinessId == businessId)
                .OrderBy(c => c.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);
    }
}
