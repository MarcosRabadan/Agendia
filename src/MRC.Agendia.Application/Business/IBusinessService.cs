using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Business
{
    public interface IBusinessService
    {
        /// <summary>Gets a paged list of active businesses as public DTOs.</summary>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A paged result of public business DTOs.</returns>
        Task<PagedResult<BusinessPublicDto>> GetPagedPublicAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>Gets an active business by its identifier as a public DTO.</summary>
        /// <param name="id">The business identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The public business DTO, or <c>null</c> if not found or inactive.</returns>
        Task<BusinessPublicDto?> GetPublicByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Creates a new business.</summary>
        /// <param name="dto">The data used to create the business.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The created business DTO.</returns>
        Task<BusinessDto> CreateAsync(CreateBusinessDto dto, CancellationToken cancellationToken = default);

        /// <summary>Updates an existing business.</summary>
        /// <param name="dto">The data used to update the business, including its identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The updated business DTO.</returns>
        Task<BusinessDto> UpdateAsync(UpdateBusinessDto dto, CancellationToken cancellationToken = default);

        /// <summary>Soft-deletes a business by its identifier.</summary>
        /// <param name="id">The business identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns><c>true</c> when the business is deleted.</returns>
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Restores a previously soft-deleted business by its identifier.</summary>
        /// <param name="id">The business identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns><c>true</c> when the business is restored or was not deleted.</returns>
        Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);
    }
}
