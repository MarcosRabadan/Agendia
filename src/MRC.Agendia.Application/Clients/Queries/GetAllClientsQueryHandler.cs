using MediatR;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Clients.Queries
{
    public class GetAllClientsQueryHandler : IRequestHandler<GetAllClientsQuery, IEnumerable<ClientDto>>
    {
        private readonly IClientRepository _repository;

        public GetAllClientsQueryHandler(IClientRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ClientDto>> Handle(GetAllClientsQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();
            return entities.Select(e => new ClientDto(e.Id, e.Name, e.Phone, e.Email));
        }
    }
}
