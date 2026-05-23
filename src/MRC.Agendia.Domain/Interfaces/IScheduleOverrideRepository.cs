using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IScheduleOverrideRepository
    {
        Task<ScheduleOverride?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ScheduleOverride?> GetByIdWithSlotsAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<ScheduleOverride>> GetByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ScheduleOverride>> GetByBusinessIdAndDateRangeAsync(int businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
        Task<ScheduleOverride?> GetByBusinessIdAndDateAsync(int businessId, DateOnly date, CancellationToken cancellationToken = default);
        Task AddAsync(ScheduleOverride scheduleOverride, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<ScheduleOverride> overrides, CancellationToken cancellationToken = default);
        void Update(ScheduleOverride scheduleOverride);
        void Delete(ScheduleOverride scheduleOverride);
    }
}
