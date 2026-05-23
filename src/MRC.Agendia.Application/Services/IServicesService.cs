using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Services.DTO;

namespace MRC.Agendia.Application.Services
{
    public interface IServicesService
    {
        Task<PagedResult<ServiceDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<ServiceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ServiceDto> CreateAsync(CreateServiceDto dto, CancellationToken cancellationToken = default);
        Task<ServiceDto> UpdateAsync(UpdateServiceDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);
    }
}
