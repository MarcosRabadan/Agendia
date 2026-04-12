using MRC.Agendia.Application.Clients.DTO;

namespace MRC.Agendia.Application.Clients
{
    public interface IClientService
    {
        Task<IEnumerable<ClientDto>> GetAllAsync();
        Task<ClientDto?> GetByIdAsync(int id);
        Task<ClientDto> CreateAsync(CreateClientDto dto);
        Task<ClientDto> UpdateAsync(UpdateClientDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
