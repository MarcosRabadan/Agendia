using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class DeviceTokenRepository : RepositoryBase<DeviceToken>, IDeviceTokenRepository
    {
        public DeviceTokenRepository(AgendiaDbContext context) : base(context)
        {
        }

        public Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
            => Set.FirstOrDefaultAsync(d => d.Token == token, cancellationToken);

        public async Task<IReadOnlyList<string>> GetTokensByUserIdAsync(string userId, CancellationToken cancellationToken = default)
            => await Set
                .AsNoTracking()
                .Where(d => d.UserId == userId)
                .Select(d => d.Token)
                .ToListAsync(cancellationToken);
    }
}
