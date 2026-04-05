using MediatR;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Business.Commands
{
    public record CreateBusinessCommand(CreateBusinessDto Dto) : IRequest<BusinessDto>;
    public record UpdateBusinessCommand(UpdateBusinessDto Dto) : IRequest<BusinessDto>;
    public record DeleteBusinessCommand(int Id) : IRequest<bool>;

    public class CreateBusinessCommandHandler : IRequestHandler<CreateBusinessCommand, BusinessDto>
    {
        private readonly IBusinessRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateBusinessCommandHandler(IBusinessRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<BusinessDto> Handle(CreateBusinessCommand request, CancellationToken cancellationToken)
        {
            var entity = new Domain.Entities.Business
            {
                Name = request.Dto.Name,
                Description = request.Dto.Description,
                Address = request.Dto.Address,
                Phone = request.Dto.Phone,
                Email = request.Dto.Email,
                IsActive = true
            };

            await _repository.AddAsync(entity);
            await _unitOfWork.Save();

            return new BusinessDto(entity.Id, entity.Name, entity.Description, entity.Address, entity.Phone, entity.Email, entity.IsActive);
        }
    }

    public class UpdateBusinessCommandHandler : IRequestHandler<UpdateBusinessCommand, BusinessDto>
    {
        private readonly IBusinessRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateBusinessCommandHandler(IBusinessRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<BusinessDto> Handle(UpdateBusinessCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Dto.Id)
                ?? throw new KeyNotFoundException($"Business with Id {request.Dto.Id} not found.");

            entity.Name = request.Dto.Name;
            entity.Description = request.Dto.Description;
            entity.Address = request.Dto.Address;
            entity.Phone = request.Dto.Phone;
            entity.Email = request.Dto.Email;
            entity.IsActive = request.Dto.IsActive;

            _repository.Update(entity);
            await _unitOfWork.Save();

            return new BusinessDto(entity.Id, entity.Name, entity.Description, entity.Address, entity.Phone, entity.Email, entity.IsActive);
        }
    }

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
