using MediatR;

namespace MRC.Agendia.Application.Employees.Commands
{
    public record DeleteEmployeeCommand(int Id) : IRequest<bool>;
}
