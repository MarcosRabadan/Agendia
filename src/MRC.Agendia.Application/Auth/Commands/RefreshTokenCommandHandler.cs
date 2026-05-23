using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Commands
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
    {
        private readonly IAuthService _service;
        public RefreshTokenCommandHandler(IAuthService service) { _service = service; }
        public Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
            => _service.RefreshAsync(request.RefreshToken, cancellationToken);
    }
}
