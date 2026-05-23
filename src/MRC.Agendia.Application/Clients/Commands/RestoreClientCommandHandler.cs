using MediatR;

namespace MRC.Agendia.Application.Clients.Commands
{
    public class RestoreClientCommandHandler : IRequestHandler<RestoreClientCommand, bool>
    {
        private readonly IClientService _service;

        public RestoreClientCommandHandler(IClientService service)
        {
            _service = service;
        }

        public async Task<bool> Handle(RestoreClientCommand request, CancellationToken cancellationToken)
        {
            return await _service.RestoreAsync(request.Id, cancellationToken);
        }
    }
}
