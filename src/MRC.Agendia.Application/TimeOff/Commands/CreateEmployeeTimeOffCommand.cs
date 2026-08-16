using MediatR;
using MRC.Agendia.Application.TimeOff.DTO;

namespace MRC.Agendia.Application.TimeOff.Commands
{
    /// <summary>Blocks an employee's agenda for a range.</summary>
    public record CreateEmployeeTimeOffCommand(Guid EmployeeId, CreateEmployeeTimeOffDto Dto)
        : IRequest<CreateEmployeeTimeOffResultDto>;
}
