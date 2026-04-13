using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IScheduleOverrideRepository
    {
        Task<ScheduleOverride?> GetByIdAsync(int id);
        Task<ScheduleOverride?> GetByIdWithSlotsAsync(int id);
        Task<IEnumerable<ScheduleOverride>> GetByBusinessIdAsync(int businessId);
        Task<IEnumerable<ScheduleOverride>> GetByBusinessIdAndDateRangeAsync(int businessId, DateOnly from, DateOnly to);
        Task<ScheduleOverride?> GetByBusinessIdAndDateAsync(int businessId, DateOnly date);
        Task AddAsync(ScheduleOverride scheduleOverride);
        Task AddRangeAsync(IEnumerable<ScheduleOverride> overrides);
        void Update(ScheduleOverride scheduleOverride);
        void Delete(ScheduleOverride scheduleOverride);
        void DeleteRange(IEnumerable<ScheduleOverride> overrides);
    }
}
