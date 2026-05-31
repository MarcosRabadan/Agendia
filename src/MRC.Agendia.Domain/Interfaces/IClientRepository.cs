using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IClientRepository
    {
        /// <summary>Gets a tracked client by id, honouring the soft-delete filter.</summary>
        /// <param name="id">Client id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The client, or null when soft-deleted or missing.</returns>
        Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Gets a client by id ignoring the soft-delete filter (for restore).</summary>
        /// <param name="id">Client id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The client even if soft-deleted, or null when missing.</returns>
        Task<Client?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Gets the (untracked) client linked to an identity user id, honouring the soft-delete filter.</summary>
        /// <param name="userId">Identity user id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The client, or null when none is linked or it is soft-deleted.</returns>
        Task<Client?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>Gets a page of clients ordered by id. Untracked.</summary>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The page of clients and the total count.</returns>
        Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>Gets a page of the clients owned by a business, ordered by id. Untracked.</summary>
        /// <param name="businessId">Owning business id.</param>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The page of the business's clients and the total count.</returns>
        Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedByBusinessIdAsync(int businessId,
                                                                                      int page,
                                                                                      int pageSize,
                                                                                      CancellationToken cancellationToken = default);

        /// <summary>Adds a new client to the context.</summary>
        /// <param name="client">The client to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task AddAsync(Client client, CancellationToken cancellationToken = default);

        /// <summary>Marks a client as modified.</summary>
        /// <param name="client">The client to update.</param>
        void Update(Client client);

        /// <summary>Removes a client (soft-deleted by the save interceptor).</summary>
        /// <param name="client">The client to delete.</param>
        void Delete(Client client);
    }
}
