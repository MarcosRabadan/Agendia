using MediatR;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Business.Queries
{
    public class GetBusinessByIdQueryHandler : IRequestHandler<GetBusinessByIdQuery, BusinessDto?>
    {
        private readonly IBusinessRepository _repository;

        public GetBusinessByIdQueryHandler(IBusinessRepository repository)
        {
            _repository = repository;
        }

        public async Task<BusinessDto?> Handle(GetBusinessByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity is null) return null;

            return new BusinessDto(entity.Id, entity.Name, entity.Description, entity.Address, entity.Phone, entity.Email, entity.IsActive);
        }
    }
}
