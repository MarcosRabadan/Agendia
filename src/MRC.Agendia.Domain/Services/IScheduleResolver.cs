using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Services
{
    public interface IScheduleResolver
    {
        /// <summary>Resolves the effective schedule of a business for a single day from the persisted templates and overrides.</summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="date">Day to resolve.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The effective schedule for that day.</returns>
        Task<EffectiveSchedule> GetEffectiveScheduleAsync(int businessId, DateOnly date, CancellationToken cancellationToken = default);

        /// <summary>Resolves the effective schedule of a business for each day in a date range, loading the data once and resolving in memory.</summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="from">Inclusive start of the range.</param>
        /// <param name="to">Inclusive end of the range.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>One effective schedule per date in the range.</returns>
        Task<IEnumerable<EffectiveSchedule>> GetEffectiveSchedulesAsync(int businessId,
                                                                        DateOnly from,
                                                                        DateOnly to,
                                                                        CancellationToken cancellationToken = default);

        /// <summary>
        /// Pure, in-memory resolution from the supplied templates and overrides
        /// (no DB access). Used to preview a schedule that has not been persisted.
        /// An override for the date wins over templates; otherwise the template
        /// covering the date provides the weekly slots.
        /// </summary>
        /// <param name="templates">Candidate templates to resolve from.</param>
        /// <param name="overrides">Candidate overrides to resolve from.</param>
        /// <param name="date">Day to resolve.</param>
        /// <returns>The effective schedule for that day.</returns>
        EffectiveSchedule Resolve(
            IEnumerable<ScheduleTemplate> templates,
            IEnumerable<ScheduleOverride> overrides,
            DateOnly date);

        /// <summary>
        /// Pure, in-memory resolution of a whole date range from the supplied
        /// templates and overrides (no DB access). Indexes the overrides by date
        /// once so each day is resolved in O(1). Shared by the calendar lookup and
        /// the schedule preview.
        /// </summary>
        /// <param name="templates">Candidate templates to resolve from.</param>
        /// <param name="overrides">Candidate overrides to resolve from (first wins on duplicate dates).</param>
        /// <param name="from">Inclusive start of the range.</param>
        /// <param name="to">Inclusive end of the range.</param>
        /// <returns>One effective schedule per date in the range.</returns>
        IEnumerable<EffectiveSchedule> ResolveRange(IEnumerable<ScheduleTemplate> templates,
                                                    IEnumerable<ScheduleOverride> overrides,
                                                    DateOnly from,
                                                    DateOnly to);
    }
}
