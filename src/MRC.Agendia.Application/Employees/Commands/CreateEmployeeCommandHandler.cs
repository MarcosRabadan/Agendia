using MediatR;
using MRC.Agendia.Application.Employees.DTO;

namespace MRC.Agendia.Application.Employees.Commands
{
    public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, EmployeeDto>
    {
        private readonly IEmployeeService _service;

        public CreateEmployeeCommandHandler(IEmployeeService service)
        {
            _service = service;
        }

        public async Task<EmployeeDto> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            return await _service.CreateAsync(request.Dto);
        }
    }
}
