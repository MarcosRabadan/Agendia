using MediatR;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.BusinessSchedule.Commands
{
    public class DeleteBusinessScheduleCommandHandler : IRequestHandler<DeleteBusinessScheduleCommand, bool>
    {
        private readonly IBusinessScheduleRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteBusinessScheduleCommandHandler(IBusinessScheduleRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteBusinessScheduleCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id)
                ?? throw new KeyNotFoundException($"BusinessSchedule with Id {request.Id} not found.");

            _repository.Delete(entity);
            await _unitOfWork.Save();
            return true;
        }
    }
}
