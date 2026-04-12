using MRC.Agendia.Application.Services.DTO;

namespace MRC.Agendia.Application.Services
{
    public interface IServicesService
    {
        Task<IEnumerable<ServiceDto>> GetAllAsync();
        Task<ServiceDto?> GetByIdAsync(int id);
        Task<ServiceDto> CreateAsync(CreateServiceDto dto);
        Task<ServiceDto> UpdateAsync(UpdateServiceDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
