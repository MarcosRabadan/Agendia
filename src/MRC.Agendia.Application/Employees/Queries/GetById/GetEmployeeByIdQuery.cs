using MediatR;
using MRC.Agendia.Application.Employees.DTO;

namespace MRC.Agendia.Application.Employees.Queries.GetById
{
    public record GetEmployeeByIdQuery(int Id) : IRequest<EmployeeDto?>;
}
