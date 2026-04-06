using MediatR;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Employees.Commands
{
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
