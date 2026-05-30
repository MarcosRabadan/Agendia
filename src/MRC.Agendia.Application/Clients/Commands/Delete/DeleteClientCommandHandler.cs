using MediatR;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Application.Clients.Commands.Delete
{
    public class DeleteClientCommandHandler : IRequestHandler<DeleteClientCommand, bool>
    {
        private readonly IClientService _service;
        private readonly IResourceAuthorizationService _auth;

        public DeleteClientCommandHandler(IClientService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<bool> Handle(DeleteClientCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageClientAsync(request.Id, cancellationToken);
            return await _service.DeleteAsync(request.Id, cancellationToken);
        }
    }
}
