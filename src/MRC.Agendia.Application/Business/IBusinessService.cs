using MRC.Agendia.Application.Business.DTO;

namespace MRC.Agendia.Application.Business
{
    public interface IBusinessService
    {
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
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Restores a previously soft-deleted business by its identifier.</summary>
        /// <param name="id">The business identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns><c>true</c> when the business is restored or was not deleted.</returns>
        Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
