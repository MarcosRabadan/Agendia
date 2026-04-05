using MediatR;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Employees.Queries
{
    public record GetEmployeeByIdQuery(int Id) : IRequest<EmployeeDto?>;
    public record GetAllEmployeesQuery() : IRequest<IEnumerable<EmployeeDto>>;

    public class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, EmployeeDto?>
    {
        private readonly IEmployeeRepository _repository;

        public GetEmployeeByIdQueryHandler(IEmployeeRepository repository)
        {
            _repository = repository;
        }

        public async Task<EmployeeDto?> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity is null) return null;

            return new EmployeeDto(entity.Id, entity.BusinessId, entity.FullName, entity.Email, entity.Phone, entity.IsActive);
        }
    }

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
