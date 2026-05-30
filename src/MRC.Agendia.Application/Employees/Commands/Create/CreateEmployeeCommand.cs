using MediatR;
using MRC.Agendia.Application.Employees.DTO;

namespace MRC.Agendia.Application.Employees.Commands.Create
{
    public record CreateEmployeeCommand(CreateEmployeeDto Dto) : IRequest<EmployeeDto>;
}
