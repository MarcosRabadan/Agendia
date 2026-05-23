using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Services
{
    public interface IScheduleResolver
    {
        Task<EffectiveSchedule> GetEffectiveScheduleAsync(int businessId, DateOnly date, CancellationToken cancellationToken = default);
        Task<IEnumerable<EffectiveSchedule>> GetEffectiveSchedulesAsync(int businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pure, in-memory resolution from the supplied templates and overrides
        /// (no DB access). Used to preview a schedule that has not been persisted.
        /// An override for the date wins over templates; otherwise the template
        /// covering the date provides the weekly slots.
        /// </summary>
        EffectiveSchedule Resolve(
            IEnumerable<ScheduleTemplate> templates,
            IEnumerable<ScheduleOverride> overrides,
            DateOnly date);
    }
}
