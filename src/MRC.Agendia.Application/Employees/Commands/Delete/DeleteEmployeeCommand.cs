using MediatR;

namespace MRC.Agendia.Application.Employees.Commands.Delete
{
    public record DeleteEmployeeCommand(int Id) : IRequest<bool>;
}
