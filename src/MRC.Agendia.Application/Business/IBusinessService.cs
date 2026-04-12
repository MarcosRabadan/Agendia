using MRC.Agendia.Application.Business.DTO;

namespace MRC.Agendia.Application.Business
{
    public interface IBusinessService
    {
        Task<IEnumerable<BusinessDto>> GetAllAsync();
        Task<BusinessDto?> GetByIdAsync(int id);
        Task<BusinessDto> CreateAsync(CreateBusinessDto dto);
        Task<BusinessDto> UpdateAsync(UpdateBusinessDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
