using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Employees.DTO;

namespace MRC.Agendia.Application.Employees.Queries
{
    public class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, EmployeeDto?>
    {
        private readonly IEmployeeService _service;
        private readonly IResourceAuthorizationService _auth;

        public GetEmployeeByIdQueryHandler(IEmployeeService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<EmployeeDto?> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanViewEmployeeAsync(request.Id, cancellationToken);
            return await _service.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}
