using MediatR;

namespace MRC.Agendia.Application.Employees.Commands.Delete
{
    public record DeleteEmployeeCommand(Guid Id) : IRequest<bool>;
}
