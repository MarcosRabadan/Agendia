using MediatR;
using MRC.Agendia.Application.Employees.DTO;

namespace MRC.Agendia.Application.Employees.Commands
{
    public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, EmployeeDto>
    {
        private readonly IEmployeeService _service;

        public UpdateEmployeeCommandHandler(IEmployeeService service)
        {
            _service = service;
        }

        public async Task<EmployeeDto> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            return await _service.UpdateAsync(request.Dto);
        }
    }
}
