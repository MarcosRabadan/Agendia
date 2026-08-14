using MediatR;

namespace MRC.Agendia.Application.Employees.Commands.Restore
{
    public record RestoreEmployeeCommand(Guid Id) : IRequest<bool>;
}
