using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class ScheduleTemplateRepository : RepositoryBase<ScheduleTemplate>, IScheduleTemplateRepository
    {
        public ScheduleTemplateRepository(AgendiaDbContext context) : base(context)
        {
        }

        public async Task<ScheduleTemplate?> GetByIdWithSlotsAsync(int id, CancellationToken cancellationToken = default)
            => await Set
                .Include(st => st.WeeklySlots)
                .FirstOrDefaultAsync(st => st.Id == id, cancellationToken);

        public async Task<IEnumerable<ScheduleTemplate>> GetByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
            => await Set
                .Include(st => st.WeeklySlots)
                .Where(st => st.BusinessId == businessId)
                .OrderBy(st => st.EffectiveFrom)
                .ToListAsync(cancellationToken);

        public async Task<ScheduleTemplate?> GetEffectiveTemplateAsync(int businessId, DateOnly date, CancellationToken cancellationToken = default)
            => await Set
                .Include(st => st.WeeklySlots)
                .Where(st => st.BusinessId == businessId
                    && st.EffectiveFrom <= date
                    && st.EffectiveTo >= date)
                .OrderByDescending(st => st.IsDefault ? 0 : 1)
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<bool> HasOverlappingTemplateAsync(int businessId, DateOnly from, DateOnly to, int? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = Set
                .Where(st => st.BusinessId == businessId
                    && st.EffectiveFrom <= to
                    && st.EffectiveTo >= from);

            if (excludeId.HasValue)
                query = query.Where(st => st.Id != excludeId.Value);

            return await query.AnyAsync(cancellationToken);
        }
    }
}
