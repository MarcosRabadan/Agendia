using MediatR;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Services.Commands
{
    public class UpdateServiceCommandHandler : IRequestHandler<UpdateServiceCommand, ServiceDto>
    {
        private readonly IServiceRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateServiceCommandHandler(IServiceRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceDto> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Dto.Id)
                ?? throw new KeyNotFoundException($"Service with Id {request.Dto.Id} not found.");

            entity.BusinessId = request.Dto.BusinessId;
            entity.Name = request.Dto.Name;
            entity.Description = request.Dto.Description;
            entity.DurationMinutes = request.Dto.DurationMinutes;
            entity.Price = request.Dto.Price;

            _repository.Update(entity);
            await _unitOfWork.Save();

            return new ServiceDto(entity.Id, entity.BusinessId, entity.Name, entity.Description, entity.DurationMinutes, entity.Price);
        }
    }
}
