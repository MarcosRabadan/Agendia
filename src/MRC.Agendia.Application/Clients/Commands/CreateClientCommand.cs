using MediatR;
using MRC.Agendia.Application.Clients.DTO;

namespace MRC.Agendia.Application.Clients.Commands
{
    public record CreateClientCommand(CreateClientDto Dto) : IRequest<ClientDto>;
}
