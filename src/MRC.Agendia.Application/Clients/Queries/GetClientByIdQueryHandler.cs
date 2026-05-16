using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Clients.DTO;

namespace MRC.Agendia.Application.Clients.Queries
{
    public class GetClientByIdQueryHandler : IRequestHandler<GetClientByIdQuery, ClientDto?>
    {
        private readonly IClientService _service;
        private readonly IResourceAuthorizationService _auth;

        public GetClientByIdQueryHandler(IClientService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<ClientDto?> Handle(GetClientByIdQuery request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageClientAsync(request.Id);
            return await _service.GetByIdAsync(request.Id);
        }
    }
}
