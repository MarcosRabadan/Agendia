using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Services
{
    public interface IScheduleResolver
    {
        Task<EffectiveSchedule> GetEffectiveScheduleAsync(int businessId, DateOnly date);
        Task<IEnumerable<EffectiveSchedule>> GetEffectiveSchedulesAsync(int businessId, DateOnly from, DateOnly to);
    }
}
