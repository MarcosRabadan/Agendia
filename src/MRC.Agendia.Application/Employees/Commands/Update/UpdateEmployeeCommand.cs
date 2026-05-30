using MediatR;
using MRC.Agendia.Application.Employees.DTO;

namespace MRC.Agendia.Application.Employees.Commands.Update
{
    public record UpdateEmployeeCommand(UpdateEmployeeDto Dto) : IRequest<EmployeeDto>;
}
