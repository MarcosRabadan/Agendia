using MediatR;

namespace MRC.Agendia.Application.Auth.Commands
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Unit>
    {
        private readonly IAuthService _service;
        public ForgotPasswordCommandHandler(IAuthService service) { _service = service; }
        public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            await _service.ForgotPasswordAsync(request.Dto);
            return Unit.Value;
        }
    }
}
