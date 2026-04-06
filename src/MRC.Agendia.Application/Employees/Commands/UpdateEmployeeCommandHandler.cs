using MediatR;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Employees.Commands
{
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
}
