using MediatR;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Application.Employees.Commands
{
    public class DeleteEmployeeCommandHandler : IRequestHandler<DeleteEmployeeCommand, bool>
    {
        private readonly IEmployeeService _service;
        private readonly IResourceAuthorizationService _auth;

        public DeleteEmployeeCommandHandler(IEmployeeService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<bool> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanDeleteEmployeeAsync(request.Id);
            return await _service.DeleteAsync(request.Id);
        }
    }
}
