using MediatR;

namespace MRC.Agendia.Application.Employees.Commands
{
    public class DeleteEmployeeCommandHandler : IRequestHandler<DeleteEmployeeCommand, bool>
    {
        private readonly IEmployeeService _service;

        public DeleteEmployeeCommandHandler(IEmployeeService service)
        {
            _service = service;
        }

        public async Task<bool> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
        {
            return await _service.DeleteAsync(request.Id);
        }
    }
}
