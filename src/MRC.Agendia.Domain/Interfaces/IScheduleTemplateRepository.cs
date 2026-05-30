using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IScheduleTemplateRepository
    {
        /// <summary>Gets a tracked schedule template by id.</summary>
        /// <param name="id">Template id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The template, or null when missing.</returns>
        Task<ScheduleTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Gets a tracked schedule template by id with its weekly slots loaded.</summary>
        /// <param name="id">Template id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The template with its slots, or null when missing.</returns>
        Task<ScheduleTemplate?> GetByIdWithSlotsAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Gets a business's schedule templates with their weekly slots, ordered by effective-from date. Untracked.</summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The business's templates.</returns>
        Task<IEnumerable<ScheduleTemplate>> GetByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the template effective for a business on a given date (its date range covers it),
        /// with weekly slots. Ties are broken in favour of the default template. Untracked.
        /// </summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="date">The date to resolve.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The effective template, or null when none covers the date.</returns>
        Task<ScheduleTemplate?> GetEffectiveTemplateAsync(int businessId, DateOnly date, CancellationToken cancellationToken = default);

        /// <summary>Checks whether a business already has a template whose date range overlaps [from, to], optionally excluding one.</summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="from">Range start (inclusive).</param>
        /// <param name="to">Range end (inclusive).</param>
        /// <param name="excludeId">Template id to ignore (e.g. the one being edited), or null.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True when an overlapping template exists.</returns>
        Task<bool> HasOverlappingTemplateAsync(int businessId, DateOnly from, DateOnly to, int? excludeId = null, CancellationToken cancellationToken = default);

        /// <summary>Adds a new schedule template to the context.</summary>
        /// <param name="template">The template to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task AddAsync(ScheduleTemplate template, CancellationToken cancellationToken = default);

        /// <summary>Marks a schedule template as modified.</summary>
        /// <param name="template">The template to update.</param>
        void Update(ScheduleTemplate template);

        /// <summary>Removes a schedule template from the context (hard delete; templates are not soft-deletable).</summary>
        /// <param name="template">The template to delete.</param>
        void Delete(ScheduleTemplate template);
    }
}
