using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Services.DTO;

namespace MRC.Agendia.Application.Services
{
    public interface IServicesService
    {
        Task<PagedResult<ServiceDto>> GetPagedAsync(int page, int pageSize);
        Task<ServiceDto?> GetByIdAsync(int id);
        Task<ServiceDto> CreateAsync(CreateServiceDto dto);
        Task<ServiceDto> UpdateAsync(UpdateServiceDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
