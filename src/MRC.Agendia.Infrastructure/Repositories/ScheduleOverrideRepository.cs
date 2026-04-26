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

        public async Task<ScheduleOverride?> GetByIdAsync(int id)
            => await _context.ScheduleOverrides.FindAsync(id);

        public async Task<ScheduleOverride?> GetByIdWithSlotsAsync(int id)
            => await _context.ScheduleOverrides
                .Include(so => so.CustomSlots)
                .FirstOrDefaultAsync(so => so.Id == id);

        public async Task<IEnumerable<ScheduleOverride>> GetByBusinessIdAsync(int businessId)
            => await _context.ScheduleOverrides
                .Include(so => so.CustomSlots)
                .Where(so => so.BusinessId == businessId)
                .OrderBy(so => so.Date)
                .ToListAsync();

        public async Task<IEnumerable<ScheduleOverride>> GetByBusinessIdAndDateRangeAsync(int businessId, DateOnly from, DateOnly to)
            => await _context.ScheduleOverrides
                .Include(so => so.CustomSlots)
                .Where(so => so.BusinessId == businessId
                    && so.Date >= from
                    && so.Date <= to)
                .OrderBy(so => so.Date)
                .ToListAsync();

        public async Task<ScheduleOverride?> GetByBusinessIdAndDateAsync(int businessId, DateOnly date)
            => await _context.ScheduleOverrides
                .Include(so => so.CustomSlots)
                .FirstOrDefaultAsync(so => so.BusinessId == businessId && so.Date == date);

        public async Task AddAsync(ScheduleOverride scheduleOverride)
            => await _context.ScheduleOverrides.AddAsync(scheduleOverride);

        public async Task AddRangeAsync(IEnumerable<ScheduleOverride> overrides)
            => await _context.ScheduleOverrides.AddRangeAsync(overrides);

        public void Update(ScheduleOverride scheduleOverride)
            => _context.ScheduleOverrides.Update(scheduleOverride);

        public void Delete(ScheduleOverride scheduleOverride)
            => _context.ScheduleOverrides.Remove(scheduleOverride);

        public void DeleteRange(IEnumerable<ScheduleOverride> overrides)
            => _context.ScheduleOverrides.RemoveRange(overrides);
    }
}
