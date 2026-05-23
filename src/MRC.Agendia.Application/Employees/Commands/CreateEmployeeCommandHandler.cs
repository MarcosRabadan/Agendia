using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Employees.DTO;

namespace MRC.Agendia.Application.Employees.Commands
{
    public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, EmployeeDto>
    {
        private readonly IEmployeeService _service;
        private readonly IResourceAuthorizationService _auth;

        public CreateEmployeeCommandHandler(IEmployeeService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<EmployeeDto> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            // Without this check a BusinessOwner of business A could create an
            // employee inside business B just by passing BusinessId = B.
            await _auth.EnsureCanManageBusinessResourcesAsync(request.Dto.BusinessId, cancellationToken);
            return await _service.CreateAsync(request.Dto, cancellationToken);
        }
    }
}
