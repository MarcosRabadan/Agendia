using MediatR;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Business.Queries
{
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
