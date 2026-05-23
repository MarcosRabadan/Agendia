using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IScheduleTemplateRepository
    {
        Task<ScheduleTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ScheduleTemplate?> GetByIdWithSlotsAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<ScheduleTemplate>> GetByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
        Task<ScheduleTemplate?> GetEffectiveTemplateAsync(int businessId, DateOnly date, CancellationToken cancellationToken = default);
        Task<bool> HasOverlappingTemplateAsync(int businessId, DateOnly from, DateOnly to, int? excludeId = null, CancellationToken cancellationToken = default);
        Task AddAsync(ScheduleTemplate template, CancellationToken cancellationToken = default);
        void Update(ScheduleTemplate template);
        void Delete(ScheduleTemplate template);
    }
}
