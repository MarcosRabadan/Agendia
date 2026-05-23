using MediatR;

namespace MRC.Agendia.Application.Auth.Commands
{
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
    {
        private readonly IAuthService _service;
        public LogoutCommandHandler(IAuthService service) { _service = service; }
        public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            await _service.LogoutAsync(request.RefreshToken, request.UserId, cancellationToken);
            return Unit.Value;
        }
    }
}
