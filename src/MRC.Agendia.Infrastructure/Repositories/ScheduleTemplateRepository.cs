using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class ScheduleTemplateRepository : IScheduleTemplateRepository
    {
        private readonly AgendiaDbContext _context;

        public ScheduleTemplateRepository(AgendiaDbContext context)
        {
            _context = context;
        }

        public async Task<ScheduleTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => await _context.ScheduleTemplates.FindAsync(new object?[] { id }, cancellationToken);

        public async Task<ScheduleTemplate?> GetByIdWithSlotsAsync(int id, CancellationToken cancellationToken = default)
            => await _context.ScheduleTemplates
                .Include(st => st.WeeklySlots)
                .FirstOrDefaultAsync(st => st.Id == id, cancellationToken);

        public async Task<IEnumerable<ScheduleTemplate>> GetByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
            => await _context.ScheduleTemplates
                .Include(st => st.WeeklySlots)
                .Where(st => st.BusinessId == businessId)
                .OrderBy(st => st.EffectiveFrom)
                .ToListAsync(cancellationToken);

        public async Task<ScheduleTemplate?> GetEffectiveTemplateAsync(int businessId, DateOnly date, CancellationToken cancellationToken = default)
            => await _context.ScheduleTemplates
                .Include(st => st.WeeklySlots)
                .Where(st => st.BusinessId == businessId
                    && st.EffectiveFrom <= date
                    && st.EffectiveTo >= date)
                .OrderByDescending(st => st.IsDefault ? 0 : 1)
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<bool> HasOverlappingTemplateAsync(int businessId, DateOnly from, DateOnly to, int? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.ScheduleTemplates
                .Where(st => st.BusinessId == businessId
                    && st.EffectiveFrom <= to
                    && st.EffectiveTo >= from);

            if (excludeId.HasValue)
                query = query.Where(st => st.Id != excludeId.Value);

            return await query.AnyAsync(cancellationToken);
        }

        public async Task AddAsync(ScheduleTemplate template, CancellationToken cancellationToken = default)
            => await _context.ScheduleTemplates.AddAsync(template, cancellationToken);

        public void Update(ScheduleTemplate template)
            => _context.ScheduleTemplates.Update(template);

        public void Delete(ScheduleTemplate template)
            => _context.ScheduleTemplates.Remove(template);
    }
}
