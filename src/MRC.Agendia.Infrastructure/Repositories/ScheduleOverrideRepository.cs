using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class ScheduleOverrideRepository : RepositoryBase<ScheduleOverride>, IScheduleOverrideRepository
    {
        public ScheduleOverrideRepository(AgendiaDbContext context) : base(context)
        {
        }

        public async Task<ScheduleOverride?> GetByIdWithSlotsAsync(int id, CancellationToken cancellationToken = default)
            => await Set
                .Include(so => so.CustomSlots)
                .FirstOrDefaultAsync(so => so.Id == id, cancellationToken);

        public async Task<IEnumerable<ScheduleOverride>> GetByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
            => await Set
                .AsNoTracking()
                .Include(so => so.CustomSlots)
                .Where(so => so.BusinessId == businessId)
                .OrderBy(so => so.Date)
                .ToListAsync(cancellationToken);

        public async Task<IEnumerable<ScheduleOverride>> GetByBusinessIdAndDateRangeAsync(int businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
            => await Set
                .AsNoTracking()
                .Include(so => so.CustomSlots)
                .Where(so => so.BusinessId == businessId
                    && so.Date >= from
                    && so.Date <= to)
                .OrderBy(so => so.Date)
                .ToListAsync(cancellationToken);

        public async Task<ScheduleOverride?> GetByBusinessIdAndDateAsync(int businessId, DateOnly date, CancellationToken cancellationToken = default)
            => await Set
                .AsNoTracking()
                .Include(so => so.CustomSlots)
                .FirstOrDefaultAsync(so => so.BusinessId == businessId && so.Date == date, cancellationToken);

        public async Task AddRangeAsync(IEnumerable<ScheduleOverride> overrides, CancellationToken cancellationToken = default)
            => await Set.AddRangeAsync(overrides, cancellationToken);
    }
}
