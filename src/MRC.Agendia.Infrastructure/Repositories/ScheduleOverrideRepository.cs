using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class ScheduleOverrideRepository : IScheduleOverrideRepository
    {
        private readonly AgendiaDbContext _context;

        public ScheduleOverrideRepository(AgendiaDbContext context)
        {
            _context = context;
        }

        public async Task<ScheduleOverride?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => await _context.ScheduleOverrides.FindAsync(new object?[] { id }, cancellationToken);

        public async Task<ScheduleOverride?> GetByIdWithSlotsAsync(int id, CancellationToken cancellationToken = default)
            => await _context.ScheduleOverrides
                .Include(so => so.CustomSlots)
                .FirstOrDefaultAsync(so => so.Id == id, cancellationToken);

        public async Task<IEnumerable<ScheduleOverride>> GetByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
            => await _context.ScheduleOverrides
                .Include(so => so.CustomSlots)
                .Where(so => so.BusinessId == businessId)
                .OrderBy(so => so.Date)
                .ToListAsync(cancellationToken);

        public async Task<IEnumerable<ScheduleOverride>> GetByBusinessIdAndDateRangeAsync(int businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
            => await _context.ScheduleOverrides
                .Include(so => so.CustomSlots)
                .Where(so => so.BusinessId == businessId
                    && so.Date >= from
                    && so.Date <= to)
                .OrderBy(so => so.Date)
                .ToListAsync(cancellationToken);

        public async Task<ScheduleOverride?> GetByBusinessIdAndDateAsync(int businessId, DateOnly date, CancellationToken cancellationToken = default)
            => await _context.ScheduleOverrides
                .Include(so => so.CustomSlots)
                .FirstOrDefaultAsync(so => so.BusinessId == businessId && so.Date == date, cancellationToken);

        public async Task AddAsync(ScheduleOverride scheduleOverride, CancellationToken cancellationToken = default)
            => await _context.ScheduleOverrides.AddAsync(scheduleOverride, cancellationToken);

        public async Task AddRangeAsync(IEnumerable<ScheduleOverride> overrides, CancellationToken cancellationToken = default)
            => await _context.ScheduleOverrides.AddRangeAsync(overrides, cancellationToken);

        public void Update(ScheduleOverride scheduleOverride)
            => _context.ScheduleOverrides.Update(scheduleOverride);

        public void Delete(ScheduleOverride scheduleOverride)
            => _context.ScheduleOverrides.Remove(scheduleOverride);

        public void DeleteRange(IEnumerable<ScheduleOverride> overrides)
            => _context.ScheduleOverrides.RemoveRange(overrides);
    }
}
