using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Commands
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
    {
        private readonly IAuthService _service;
        public LoginCommandHandler(IAuthService service) { _service = service; }
        public Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
            => _service.LoginAsync(request.Dto);
    }
}
