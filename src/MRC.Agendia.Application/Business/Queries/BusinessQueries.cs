using MediatR;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Business.Queries
{
    public record GetBusinessByIdQuery(int Id) : IRequest<BusinessDto?>;
    public record GetAllBusinessesQuery() : IRequest<IEnumerable<BusinessDto>>;

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

    public class GetAllBusinessesQueryHandler : IRequestHandler<GetAllBusinessesQuery, IEnumerable<BusinessDto>>
    {
        private readonly IBusinessRepository _repository;

        public GetAllBusinessesQueryHandler(IBusinessRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<BusinessDto>> Handle(GetAllBusinessesQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();
            return entities.Select(e => new BusinessDto(e.Id, e.Name, e.Description, e.Address, e.Phone, e.Email, e.IsActive));
        }
    }
}
