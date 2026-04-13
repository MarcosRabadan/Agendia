using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IScheduleTemplateRepository
    {
        Task<ScheduleTemplate?> GetByIdAsync(int id);
        Task<ScheduleTemplate?> GetByIdWithSlotsAsync(int id);
        Task<IEnumerable<ScheduleTemplate>> GetByBusinessIdAsync(int businessId);
        Task<ScheduleTemplate?> GetEffectiveTemplateAsync(int businessId, DateOnly date);
        Task<bool> HasOverlappingTemplateAsync(int businessId, DateOnly from, DateOnly to, int? excludeId = null);
        Task AddAsync(ScheduleTemplate template);
        void Update(ScheduleTemplate template);
        void Delete(ScheduleTemplate template);
    }
}
