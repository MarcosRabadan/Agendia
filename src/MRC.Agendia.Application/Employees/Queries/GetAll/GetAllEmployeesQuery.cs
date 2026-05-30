using MediatR;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Employees.Queries.GetAll
{
    public record GetAllEmployeesQuery(
        int Page = PaginationConstants.DefaultPage,
        int PageSize = PaginationConstants.DefaultPageSize)
        : IRequest<PagedResult<EmployeeDto>>;
}
