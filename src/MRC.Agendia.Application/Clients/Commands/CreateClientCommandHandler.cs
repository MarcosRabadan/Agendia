using MediatR;
using MRC.Agendia.Application.Clients.DTO;

namespace MRC.Agendia.Application.Clients.Commands
{
    public class CreateClientCommandHandler : IRequestHandler<CreateClientCommand, ClientDto>
    {
        private readonly IClientService _service;

        public CreateClientCommandHandler(IClientService service)
        {
            _service = service;
        }

        public async Task<ClientDto> Handle(CreateClientCommand request, CancellationToken cancellationToken)
        {
            return await _service.CreateAsync(request.Dto, cancellationToken);
        }
    }
}
