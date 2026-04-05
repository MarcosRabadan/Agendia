using MediatR;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Clients.Queries
{
    public record GetClientByIdQuery(int Id) : IRequest<ClientDto?>;
    public record GetAllClientsQuery() : IRequest<IEnumerable<ClientDto>>;

    public class GetClientByIdQueryHandler : IRequestHandler<GetClientByIdQuery, ClientDto?>
    {
        private readonly IClientRepository _repository;

        public GetClientByIdQueryHandler(IClientRepository repository)
        {
            _repository = repository;
        }

        public async Task<ClientDto?> Handle(GetClientByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity is null) return null;

            return new ClientDto(entity.Id, entity.Name, entity.Phone, entity.Email);
        }
    }

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
