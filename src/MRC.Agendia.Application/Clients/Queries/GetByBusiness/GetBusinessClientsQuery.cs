using MediatR;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Clients.Queries.GetByBusiness
{
    public record GetBusinessClientsQuery(
        int BusinessId,
        int Page = PaginationConstants.DefaultPage,
        int PageSize = PaginationConstants.DefaultPageSize)
        : IRequest<PagedResult<ClientDto>>;
}
