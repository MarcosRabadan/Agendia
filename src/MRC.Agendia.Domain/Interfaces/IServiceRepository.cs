using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IServiceRepository
    {
        /// <summary>Gets a tracked service by id, honouring the soft-delete and business-scope filters.</summary>
        /// <param name="id">Service id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The service, or null when soft-deleted, out of scope, or missing.</returns>
        Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a non-deleted service by id, ignoring the business-scope filter so it
        /// works regardless of the caller. Untracked. Used by availability to read the
        /// service duration when laying out slots.
        /// </summary>
        /// <param name="id">Service id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The service, or null when soft-deleted or missing.</returns>
        Task<Service?> GetByIdPublicAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Gets a service by id ignoring the soft-delete filter (for restore).</summary>
        /// <param name="id">Service id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The service even if soft-deleted, or null when missing.</returns>
        Task<Service?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Adds a new service to the context.</summary>
        /// <param name="service">The service to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task AddAsync(Service service, CancellationToken cancellationToken = default);

        /// <summary>Marks a service as modified.</summary>
        /// <param name="service">The service to update.</param>
        void Update(Service service);

        /// <summary>Removes a service (soft-deleted by the save interceptor).</summary>
        /// <param name="service">The service to delete.</param>
        void Delete(Service service);
    }
}
