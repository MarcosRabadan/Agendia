using MediatR;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Services.Commands
{
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
}
