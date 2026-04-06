using MediatR;
using MRC.Agendia.Application.Clients.DTO;

namespace MRC.Agendia.Application.Clients.Queries
{
    public record GetAllClientsQuery() : IRequest<IEnumerable<ClientDto>>;
}
