using MediatR;

namespace MRC.Agendia.Application.TimeOff.Commands
{
    /// <summary>Removes a block from an employee's agenda.</summary>
    public record DeleteEmployeeTimeOffCommand(Guid EmployeeId, Guid TimeOffId) : IRequest;
}
