using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IServiceRepository
    {
        /// <summary>Gets a tracked service by id, honouring the soft-delete and business-scope filters.</summary>
        /// <param name="id">Service id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The service, or null when soft-deleted, out of scope, or missing.</returns>
        Task<Service?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a non-deleted service by id for public (catalog/availability) reads.
        /// Untracked; ignores the business-scope filter so the catalog stays open to any caller.
        /// </summary>
        /// <param name="id">Service id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The service, or null when soft-deleted or missing.</returns>
        Task<Service?> GetByIdPublicAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Gets a service by id ignoring the soft-delete filter (for restore).</summary>
        /// <param name="id">Service id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The service even if soft-deleted, or null when missing.</returns>
        Task<Service?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a page of non-deleted services for the public catalog, ordered by id.
        /// Untracked; ignores the business-scope filter.
        /// </summary>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The page of services and the total count.</returns>
        Task<(IReadOnlyList<Service> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

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
