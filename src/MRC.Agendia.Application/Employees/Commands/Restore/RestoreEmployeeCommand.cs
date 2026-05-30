using MediatR;

namespace MRC.Agendia.Application.Employees.Commands.Restore
{
    public record RestoreEmployeeCommand(int Id) : IRequest<bool>;
}
