using MediatR;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Business.Commands
{
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
}
