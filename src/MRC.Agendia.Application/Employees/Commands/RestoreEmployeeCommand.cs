using MediatR;

namespace MRC.Agendia.Application.Employees.Commands
{
    public record RestoreEmployeeCommand(int Id) : IRequest<bool>;
}
