using MediatR;

namespace MRC.Agendia.Application.Clients.Commands
{
    public class DeleteClientCommandHandler : IRequestHandler<DeleteClientCommand, bool>
    {
        private readonly IClientService _service;

        public DeleteClientCommandHandler(IClientService service)
        {
            _service = service;
        }

        public async Task<bool> Handle(DeleteClientCommand request, CancellationToken cancellationToken)
        {
            return await _service.DeleteAsync(request.Id);
        }
    }
}
