using MediatR;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Business.Queries.GetAll
{
    /// <summary>
    /// Public listing of active businesses. Returns a customer-safe projection
    /// (<see cref="BusinessPublicDto"/>) - no email, inactive businesses are
    /// filtered out upstream by <c>IBusinessRepository.GetPagedActiveAsync</c>.
    /// </summary>
    public record GetAllBusinessesQuery(
        int Page = PaginationConstants.DefaultPage,
        int PageSize = PaginationConstants.DefaultPageSize)
        : IRequest<PagedResult<BusinessPublicDto>>;
}
