using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IBusinessRepository
    {
        /// <summary>Gets a tracked business by id, honouring the soft-delete and business-scope filters.</summary>
        /// <param name="id">Business id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The business, or null when soft-deleted, out of scope, or missing.</returns>
        Task<Business?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Gets a business by id ignoring the soft-delete filter (for restore).</summary>
        /// <param name="id">Business id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The business even if soft-deleted, or null when missing.</returns>
        Task<Business?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an active, non-deleted business by id for public (anonymous) reads.
        /// Untracked; ignores the business-scope filter so it works regardless of the caller.
        /// </summary>
        /// <param name="id">Business id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The active business, or null when inactive, soft-deleted, or missing.</returns>
        Task<Business?> GetActiveByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a page of active, non-deleted businesses for the public listing, ordered by id.
        /// Untracked; ignores the business-scope filter.
        /// </summary>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The page of active businesses and the total count.</returns>
        Task<(IReadOnlyList<Business> Items, int TotalCount)> GetPagedActiveAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>Adds a new business to the context.</summary>
        /// <param name="business">The business to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task AddAsync(Business business, CancellationToken cancellationToken = default);

        /// <summary>Marks a business as modified.</summary>
        /// <param name="business">The business to update.</param>
        void Update(Business business);

        /// <summary>Removes a business (soft-deleted by the save interceptor).</summary>
        /// <param name="business">The business to delete.</param>
        void Delete(Business business);
    }
}
