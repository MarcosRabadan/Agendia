using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class ScheduleTemplateRepository : RepositoryBase<ScheduleTemplate>, IScheduleTemplateRepository
    {
        public ScheduleTemplateRepository(AgendiaDbContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public async Task<ScheduleTemplate?> GetByIdWithSlotsAsync(Guid id, CancellationToken cancellationToken = default)
            => await Set
                .Include(st => st.WeeklySlots)
                .FirstOrDefaultAsync(st => st.Id == id, cancellationToken);

        /// <inheritdoc />
        public async Task<IEnumerable<ScheduleTemplate>> GetByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
            => await Set
                .AsNoTracking()
                .Include(st => st.WeeklySlots)
                .Where(st => st.BusinessId == businessId)
                .OrderBy(st => st.EffectiveFrom)
                .ToListAsync(cancellationToken);

        /// <inheritdoc />
        public async Task<ScheduleTemplate?> GetEffectiveTemplateAsync(Guid businessId, DateOnly date, CancellationToken cancellationToken = default)
        {
            // Fetch the candidates and pick in memory with the shared rule (#307). The set is
            // one template in practice and a handful at worst, so ordering here instead of in
            // SQL costs nothing - and it keeps every caller on ONE tie-break rather than four
            // copies that happened to agree. Postgres guarantees no order past IsDefault, so
            // the SQL version could not be made deterministic without repeating the rule.
            var candidates = await Set
                .AsNoTracking()
                .Include(st => st.WeeklySlots)
                .Where(st => st.BusinessId == businessId
                    && st.EffectiveFrom <= date
                    && st.EffectiveTo >= date)
                .ToListAsync(cancellationToken);

            return ScheduleTemplateSelection.SelectFor(candidates, date);
        }

        /// <inheritdoc />
        public async Task<bool> HasOverlappingTemplateAsync(Guid businessId,
                                                            DateOnly from,
                                                            DateOnly to,
                                                            Guid? excludeId = null,
                                                            CancellationToken cancellationToken = default)
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
