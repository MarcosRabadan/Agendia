using MediatR;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Employees.Commands
{
    public record CreateEmployeeCommand(CreateEmployeeDto Dto) : IRequest<EmployeeDto>;
    public record UpdateEmployeeCommand(UpdateEmployeeDto Dto) : IRequest<EmployeeDto>;
    public record DeleteEmployeeCommand(int Id) : IRequest<bool>;

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

    public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, EmployeeDto>
    {
        private readonly IEmployeeRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateEmployeeCommandHandler(IEmployeeRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<EmployeeDto> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Dto.Id)
                ?? throw new KeyNotFoundException($"Employee with Id {request.Dto.Id} not found.");

            entity.BusinessId = request.Dto.BusinessId;
            entity.FullName = request.Dto.FullName;
            entity.Email = request.Dto.Email;
            entity.Phone = request.Dto.Phone;
            entity.IsActive = request.Dto.IsActive;

            _repository.Update(entity);
            await _unitOfWork.Save();

            return new EmployeeDto(entity.Id, entity.BusinessId, entity.FullName, entity.Email, entity.Phone, entity.IsActive);
        }
    }

    public class DeleteEmployeeCommandHandler : IRequestHandler<DeleteEmployeeCommand, bool>
    {
        private readonly IEmployeeRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteEmployeeCommandHandler(IEmployeeRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id)
                ?? throw new KeyNotFoundException($"Employee with Id {request.Id} not found.");

            _repository.Delete(entity);
            await _unitOfWork.Save();
            return true;
        }
    }
}
