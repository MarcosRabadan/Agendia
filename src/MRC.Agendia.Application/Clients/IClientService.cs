using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Clients
{
    public interface IClientService
    {
        /// <summary>Gets a paged list of clients.</summary>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A paged result of client DTOs.</returns>
        Task<PagedResult<ClientDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>Gets a client by its identifier.</summary>
        /// <param name="id">The client identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The client DTO, or <c>null</c> if not found.</returns>
        Task<ClientDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Creates a new client.</summary>
        /// <param name="dto">The data used to create the client.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The created client DTO.</returns>
        Task<ClientDto> CreateAsync(CreateClientDto dto, CancellationToken cancellationToken = default);

        /// <summary>Updates an existing client.</summary>
        /// <param name="dto">The data used to update the client, including its identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The updated client DTO.</returns>
        Task<ClientDto> UpdateAsync(UpdateClientDto dto, CancellationToken cancellationToken = default);

        /// <summary>Soft-deletes a client by its identifier.</summary>
        /// <param name="id">The client identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns><c>true</c> when the client is deleted.</returns>
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Restores a previously soft-deleted client by its identifier.</summary>
        /// <param name="id">The client identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns><c>true</c> when the client is restored or was not deleted.</returns>
        Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);
    }
}
