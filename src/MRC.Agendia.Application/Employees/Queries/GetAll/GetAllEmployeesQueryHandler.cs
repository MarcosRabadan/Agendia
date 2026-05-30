using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Employees.Queries.GetAll
{
    /// <summary>
    /// Returns the paged employee list scoped to what the caller is allowed to see:
    ///   - Admin -> every employee in the system.
    ///   - BusinessOwner -> only employees of the businesses they own.
    /// Any other role reaching here (the controller restricts to Admin/Owner, this
    /// is defense in depth) is rejected with UnauthorizedAccessException.
    /// </summary>
    public class GetAllEmployeesQueryHandler : IRequestHandler<GetAllEmployeesQuery, PagedResult<EmployeeDto>>
    {
        private readonly IEmployeeService _service;
        private readonly ICurrentUserContext _currentUser;

        public GetAllEmployeesQueryHandler(IEmployeeService service, ICurrentUserContext currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        public Task<PagedResult<EmployeeDto>> Handle(GetAllEmployeesQuery request, CancellationToken cancellationToken)
        {
            if (_currentUser.IsInRole(Roles.Admin))
            {
                return _service.GetPagedAsync(request.Page, request.PageSize, cancellationToken);
            }

            if (_currentUser.IsInRole(Roles.BusinessOwner))
            {
                var userId = _currentUser.UserId
                    ?? throw new UnauthorizedAccessException("Usuario no autenticado.");
                return _service.GetPagedByOwnerUserIdAsync(userId, request.Page, request.PageSize, cancellationToken);
            }

            throw new UnauthorizedAccessException("No tienes permiso para listar empleados.");
        }
    }
}
