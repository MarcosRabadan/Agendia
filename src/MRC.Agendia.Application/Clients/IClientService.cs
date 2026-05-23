using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Clients
{
    public interface IClientService
    {
        Task<PagedResult<ClientDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<ClientDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ClientDto> CreateAsync(CreateClientDto dto, CancellationToken cancellationToken = default);
        Task<ClientDto> UpdateAsync(UpdateClientDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);
    }
}
