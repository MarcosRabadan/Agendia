using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Employees.DTO;

namespace MRC.Agendia.Application.Employees.Commands.Update
{
    public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, EmployeeDto>
    {
        private readonly IEmployeeService _service;
        private readonly IResourceAuthorizationService _auth;

        public UpdateEmployeeCommandHandler(IEmployeeService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<EmployeeDto> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanUpdateEmployeeAsync(request.Dto.Id, cancellationToken);
            return await _service.UpdateAsync(request.Dto, cancellationToken);
        }
    }
}
