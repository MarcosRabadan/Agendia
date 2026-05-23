using MediatR;

namespace MRC.Agendia.Application.Auth.Commands
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Unit>
    {
        private readonly IAuthService _service;
        public ResetPasswordCommandHandler(IAuthService service) { _service = service; }
        public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            await _service.ResetPasswordAsync(request.Dto, cancellationToken);
            return Unit.Value;
        }
    }
}
