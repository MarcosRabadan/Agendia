using MediatR;

namespace MRC.Agendia.Application.Auth.Commands
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Unit>
    {
        private readonly IAuthService _service;
        public ChangePasswordCommandHandler(IAuthService service) { _service = service; }
        public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            await _service.ChangePasswordAsync(request.UserId, request.Dto, cancellationToken);
            return Unit.Value;
        }
    }
}
