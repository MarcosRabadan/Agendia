using MediatR;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Services.DTO;

namespace MRC.Agendia.Application.Services.Queries
{
    public record GetAllServicesQuery(
        int Page = PaginationConstants.DefaultPage,
        int PageSize = PaginationConstants.DefaultPageSize)
        : IRequest<PagedResult<ServiceDto>>;
}
