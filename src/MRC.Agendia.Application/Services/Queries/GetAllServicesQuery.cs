using MediatR;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Services.Queries
{
    public record GetAllServicesQuery(
        int Page = PaginationConstants.DefaultPage,
        int PageSize = PaginationConstants.DefaultPageSize)
        : IRequest<PagedResult<ServiceDto>>;
}
