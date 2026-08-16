using MediatR;
using MRC.Agendia.Application.TimeOff.DTO;

namespace MRC.Agendia.Application.TimeOff.Queries
{
    /// <summary>The blocks of an employee overlapping a date range.</summary>
    public record GetEmployeeTimeOffQuery(Guid EmployeeId, DateOnly From, DateOnly To)
        : IRequest<IReadOnlyList<EmployeeTimeOffDto>>;
}
