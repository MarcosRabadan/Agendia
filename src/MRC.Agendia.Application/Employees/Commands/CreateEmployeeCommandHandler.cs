using MediatR;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Employees.Commands
{
    public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, EmployeeDto>
    {
        private readonly IEmployeeRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateEmployeeCommandHandler(IEmployeeRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<EmployeeDto> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var entity = new Employee
            {
                BusinessId = request.Dto.BusinessId,
                FullName = request.Dto.FullName,
                Email = request.Dto.Email,
                Phone = request.Dto.Phone,
                IsActive = true
            };

            await _repository.AddAsync(entity);
            await _unitOfWork.Save();

            return new EmployeeDto(entity.Id, entity.BusinessId, entity.FullName, entity.Email, entity.Phone, entity.IsActive);
        }
    }
}
