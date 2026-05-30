using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Clients.DTO;

namespace MRC.Agendia.Application.Clients.Commands.Update
{
    public class UpdateClientCommandHandler : IRequestHandler<UpdateClientCommand, ClientDto>
    {
        private readonly IClientService _service;
        private readonly IResourceAuthorizationService _auth;

        public UpdateClientCommandHandler(IClientService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<ClientDto> Handle(UpdateClientCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageClientAsync(request.Dto.Id, cancellationToken);
            return await _service.UpdateAsync(request.Dto, cancellationToken);
        }
    }
}
