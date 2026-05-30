using MediatR;
using MRC.Agendia.Application.Clients.DTO;

namespace MRC.Agendia.Application.Clients.Queries.GetById
{
    public record GetClientByIdQuery(int Id) : IRequest<ClientDto?>;
}
