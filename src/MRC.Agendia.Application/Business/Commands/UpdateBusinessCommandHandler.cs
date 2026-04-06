using MediatR;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Business.Commands
{
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
}
