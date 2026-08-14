using MediatR;
using MRC.Agendia.Application.Employees.DTO;

namespace MRC.Agendia.Application.Employees.Queries.GetById
{
    public record GetEmployeeByIdQuery(Guid Id) : IRequest<EmployeeDto?>;
}
