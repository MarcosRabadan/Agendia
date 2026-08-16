using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IBusinessRepository
    {
        /// <summary>Gets a tracked business by id, honouring the soft-delete and business-scope filters.</summary>
        /// <param name="id">Business id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The business, or null when soft-deleted, out of scope, or missing.</returns>
        Task<Business?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Gets a business by id ignoring the soft-delete filter (for restore).</summary>
        /// <param name="id">Business id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The business even if soft-deleted, or null when missing.</returns>
        Task<Business?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an active, non-deleted business by id, ignoring the business-scope
        /// filter so it works regardless of the caller. Untracked. Used by availability
        /// to reject bookings against an inactive/deleted business.
        /// </summary>
        /// <param name="id">Business id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The active business, or null when inactive, soft-deleted, or missing.</returns>
        Task<Business?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken = default);

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

        /// <summary>
        /// The cancellation policy tiers of a business (#270), most notice first.
        /// Untracked: they are only read to be reported or to be replaced wholesale.
        /// </summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The tiers, or an empty list when the business defines none.</returns>
        Task<IReadOnlyList<CancellationPolicyTier>> GetCancellationTiersAsync(Guid businessId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Replaces the whole set of cancellation tiers of a business. The policy is edited
        /// as a unit, so partial CRUD would only invite half-valid states.
        /// </summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="tiers">The new tiers (empty falls back to CancellationWindowHours).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task ReplaceCancellationTiersAsync(Guid businessId, IReadOnlyList<CancellationPolicyTier> tiers, CancellationToken cancellationToken = default);
    }
}
