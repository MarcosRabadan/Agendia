using MediatR;
using MRC.Agendia.Application.Clients.DTO;

namespace MRC.Agendia.Application.Clients.Commands
{
    public class UpdateClientCommandHandler : IRequestHandler<UpdateClientCommand, ClientDto>
    {
        private readonly IClientService _service;

        public UpdateClientCommandHandler(IClientService service)
        {
            _service = service;
        }

        public async Task<ClientDto> Handle(UpdateClientCommand request, CancellationToken cancellationToken)
        {
            return await _service.UpdateAsync(request.Dto);
        }
    }
}
