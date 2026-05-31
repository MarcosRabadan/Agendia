using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Clients.DTO;

namespace MRC.Agendia.Application.Clients.Commands.Create
{
    public class CreateBusinessClientCommandHandler : IRequestHandler<CreateBusinessClientCommand, ClientDto>
    {
        private readonly IClientService _service;
        private readonly IResourceAuthorizationService _auth;

        public CreateBusinessClientCommandHandler(IClientService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<ClientDto> Handle(CreateBusinessClientCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageBusinessResourcesAsync(request.BusinessId, cancellationToken);
            return await _service.CreateForBusinessAsync(request.BusinessId, request.Dto, cancellationToken);
        }
    }
}
