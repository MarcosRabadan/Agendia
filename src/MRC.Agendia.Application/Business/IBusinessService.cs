using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Business
{
    public interface IBusinessService
    {
        Task<PagedResult<BusinessDto>> GetPagedAsync(int page, int pageSize);
        Task<BusinessDto?> GetByIdAsync(int id);
        Task<BusinessDto> CreateAsync(CreateBusinessDto dto);
        Task<BusinessDto> UpdateAsync(UpdateBusinessDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
