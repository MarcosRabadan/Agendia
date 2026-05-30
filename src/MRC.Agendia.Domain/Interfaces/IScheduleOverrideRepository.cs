using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IScheduleOverrideRepository
    {
        /// <summary>Gets a tracked schedule override by id.</summary>
        /// <param name="id">Override id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The override, or null when missing.</returns>
        Task<ScheduleOverride?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Gets a tracked schedule override by id with its custom slots loaded.</summary>
        /// <param name="id">Override id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The override with its slots, or null when missing.</returns>
        Task<ScheduleOverride?> GetByIdWithSlotsAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Gets a business's schedule overrides with their custom slots, ordered by date. Untracked.</summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The business's overrides.</returns>
        Task<IEnumerable<ScheduleOverride>> GetByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a business's schedule overrides whose date is within [from, to] (inclusive),
        /// with their custom slots, ordered by date. Untracked.
        /// </summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="from">Range start (inclusive).</param>
        /// <param name="to">Range end (inclusive).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The overrides in the range.</returns>
        Task<IEnumerable<ScheduleOverride>> GetByBusinessIdAndDateRangeAsync(
            int businessId,
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default);

        /// <summary>Gets a business's schedule override for a specific date with its custom slots. Untracked.</summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="date">The override date.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The override for that date, or null when none exists.</returns>
        Task<ScheduleOverride?> GetByBusinessIdAndDateAsync(int businessId, DateOnly date, CancellationToken cancellationToken = default);

        /// <summary>Adds a new schedule override to the context.</summary>
        /// <param name="scheduleOverride">The override to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task AddAsync(ScheduleOverride scheduleOverride, CancellationToken cancellationToken = default);

        /// <summary>Adds several schedule overrides to the context in one call.</summary>
        /// <param name="overrides">The overrides to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task AddRangeAsync(IEnumerable<ScheduleOverride> overrides, CancellationToken cancellationToken = default);

        /// <summary>Marks a schedule override as modified.</summary>
        /// <param name="scheduleOverride">The override to update.</param>
        void Update(ScheduleOverride scheduleOverride);

        /// <summary>Removes a schedule override from the context (hard delete; overrides are not soft-deletable).</summary>
        /// <param name="scheduleOverride">The override to delete.</param>
        void Delete(ScheduleOverride scheduleOverride);
    }
}
