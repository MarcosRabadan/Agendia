using MediatR;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Services.Queries
{
    public class GetAllServicesQueryHandler : IRequestHandler<GetAllServicesQuery, IEnumerable<ServiceDto>>
    {
        private readonly IServiceRepository _repository;

        public GetAllServicesQueryHandler(IServiceRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ServiceDto>> Handle(GetAllServicesQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();
            return entities.Select(e => new ServiceDto(e.Id, e.BusinessId, e.Name, e.Description, e.DurationMinutes, e.Price));
        }
    }
}
