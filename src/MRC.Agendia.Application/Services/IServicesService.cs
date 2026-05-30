using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Services.DTO;

namespace MRC.Agendia.Application.Services
{
    public interface IServicesService
    {
        /// <summary>Gets a paged list of services.</summary>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A paged result of service DTOs.</returns>
        Task<PagedResult<ServiceDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>Gets a service by its identifier from the public catalog.</summary>
        /// <param name="id">The service identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The service DTO, or <c>null</c> if not found.</returns>
        Task<ServiceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Creates a new service.</summary>
        /// <param name="dto">The data used to create the service.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The created service DTO.</returns>
        Task<ServiceDto> CreateAsync(CreateServiceDto dto, CancellationToken cancellationToken = default);

        /// <summary>Updates an existing service.</summary>
        /// <param name="dto">The data used to update the service, including its identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The updated service DTO.</returns>
        Task<ServiceDto> UpdateAsync(UpdateServiceDto dto, CancellationToken cancellationToken = default);

        /// <summary>Soft-deletes a service by its identifier.</summary>
        /// <param name="id">The service identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns><c>true</c> when the service is deleted.</returns>
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Restores a previously soft-deleted service by its identifier.</summary>
        /// <param name="id">The service identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns><c>true</c> when the service is restored or was not deleted.</returns>
        Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);
    }
}
