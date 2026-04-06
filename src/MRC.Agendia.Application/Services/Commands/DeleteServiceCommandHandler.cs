using MediatR;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Services.Commands
{
    public class DeleteServiceCommandHandler : IRequestHandler<DeleteServiceCommand, bool>
    {
        private readonly IServiceRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteServiceCommandHandler(IServiceRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteServiceCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id)
                ?? throw new KeyNotFoundException($"Service with Id {request.Id} not found.");

            _repository.Delete(entity);
            await _unitOfWork.Save();
            return true;
        }
    }
}
