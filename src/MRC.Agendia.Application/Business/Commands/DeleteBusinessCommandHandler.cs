using MediatR;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Business.Commands
{
    public class DeleteBusinessCommandHandler : IRequestHandler<DeleteBusinessCommand, bool>
    {
        private readonly IBusinessRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteBusinessCommandHandler(IBusinessRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteBusinessCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id)
                ?? throw new KeyNotFoundException($"Business with Id {request.Id} not found.");

            _repository.Delete(entity);
            await _unitOfWork.Save();
            return true;
        }
    }
}
