using MediatR;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Clients.Queries
{
    public record GetAllClientsQuery(
        int Page = PaginationConstants.DefaultPage,
        int PageSize = PaginationConstants.DefaultPageSize)
        : IRequest<PagedResult<ClientDto>>;
}
