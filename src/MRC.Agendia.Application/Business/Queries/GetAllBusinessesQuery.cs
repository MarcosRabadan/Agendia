using MediatR;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Business.Queries
{
    public record GetAllBusinessesQuery(
        int Page = PaginationConstants.DefaultPage,
        int PageSize = PaginationConstants.DefaultPageSize)
        : IRequest<PagedResult<BusinessDto>>;
}
