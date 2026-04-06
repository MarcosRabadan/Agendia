using MediatR;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Employees.Queries
{
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
}
