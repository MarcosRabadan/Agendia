using MediatR;

namespace MRC.Agendia.Application.Employees.Commands.Restore
{
    public class RestoreEmployeeCommandHandler : IRequestHandler<RestoreEmployeeCommand, bool>
    {
        private readonly IEmployeeService _service;

        public RestoreEmployeeCommandHandler(IEmployeeService service)
        {
            _service = service;
        }

        public async Task<bool> Handle(RestoreEmployeeCommand request, CancellationToken cancellationToken)
        {
            return await _service.RestoreAsync(request.Id, cancellationToken);
        }
    }
}
