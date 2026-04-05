using MediatR;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Services.Commands
{
    public record CreateServiceCommand(CreateServiceDto Dto) : IRequest<ServiceDto>;
    public record UpdateServiceCommand(UpdateServiceDto Dto) : IRequest<ServiceDto>;
    public record DeleteServiceCommand(int Id) : IRequest<bool>;

    public class CreateServiceCommandHandler : IRequestHandler<CreateServiceCommand, ServiceDto>
    {
        private readonly IServiceRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateServiceCommandHandler(IServiceRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceDto> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
        {
            var entity = new Service
            {
                BusinessId = request.Dto.BusinessId,
                Name = request.Dto.Name,
                Description = request.Dto.Description,
                DurationMinutes = request.Dto.DurationMinutes,
                Price = request.Dto.Price
            };

            await _repository.AddAsync(entity);
            await _unitOfWork.Save();

            return new ServiceDto(entity.Id, entity.BusinessId, entity.Name, entity.Description, entity.DurationMinutes, entity.Price);
        }
    }

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
