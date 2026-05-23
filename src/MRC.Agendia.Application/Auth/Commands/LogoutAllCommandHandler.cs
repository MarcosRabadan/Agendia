using MediatR;

namespace MRC.Agendia.Application.Auth.Commands
{
    public class LogoutAllCommandHandler : IRequestHandler<LogoutAllCommand, Unit>
    {
        private readonly IAuthService _service;
        public LogoutAllCommandHandler(IAuthService service) { _service = service; }
        public async Task<Unit> Handle(LogoutAllCommand request, CancellationToken cancellationToken)
        {
            await _service.LogoutAllAsync(request.UserId, cancellationToken);
            return Unit.Value;
        }
    }
}
