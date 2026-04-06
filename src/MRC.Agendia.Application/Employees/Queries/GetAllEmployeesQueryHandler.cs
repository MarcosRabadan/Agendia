using MediatR;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Employees.Queries
{
    public class GetAllEmployeesQueryHandler : IRequestHandler<GetAllEmployeesQuery, IEnumerable<EmployeeDto>>
    {
        private readonly IEmployeeRepository _repository;

        public GetAllEmployeesQueryHandler(IEmployeeRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<EmployeeDto>> Handle(GetAllEmployeesQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();
            return entities.Select(e => new EmployeeDto(e.Id, e.BusinessId, e.FullName, e.Email, e.Phone, e.IsActive));
        }
    }
}
