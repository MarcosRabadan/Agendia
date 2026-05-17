using MediatR;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Employees.DTO;

namespace MRC.Agendia.Application.Employees.Queries
{
    public class GetAllEmployeesQueryHandler : IRequestHandler<GetAllEmployeesQuery, PagedResult<EmployeeDto>>
    {
        private readonly IEmployeeService _service;

        public GetAllEmployeesQueryHandler(IEmployeeService service)
        {
            _service = service;
        }

        public Task<PagedResult<EmployeeDto>> Handle(GetAllEmployeesQuery request, CancellationToken cancellationToken)
            => _service.GetPagedAsync(request.Page, request.PageSize);
    }
}
