using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Clients
{
    public interface IClientService
    {
        Task<PagedResult<ClientDto>> GetPagedAsync(int page, int pageSize);
        Task<ClientDto?> GetByIdAsync(int id);
        Task<ClientDto> CreateAsync(CreateClientDto dto);
        Task<ClientDto> UpdateAsync(UpdateClientDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
