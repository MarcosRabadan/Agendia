using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Business
{
    public interface IBusinessService
    {
        Task<PagedResult<BusinessDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<PagedResult<BusinessPublicDto>> GetPagedPublicAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<BusinessDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<BusinessPublicDto?> GetPublicByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<BusinessDto> CreateAsync(CreateBusinessDto dto, CancellationToken cancellationToken = default);
        Task<BusinessDto> UpdateAsync(UpdateBusinessDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);
    }
}
