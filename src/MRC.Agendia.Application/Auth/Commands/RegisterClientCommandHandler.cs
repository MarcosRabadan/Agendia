using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Commands
{
    public class RegisterClientCommandHandler : IRequestHandler<RegisterClientCommand, AuthResponseDto>
    {
        private readonly IAuthService _service;
        public RegisterClientCommandHandler(IAuthService service) { _service = service; }
        public Task<AuthResponseDto> Handle(RegisterClientCommand request, CancellationToken cancellationToken)
            => _service.RegisterClientAsync(request.Dto);
    }
}
