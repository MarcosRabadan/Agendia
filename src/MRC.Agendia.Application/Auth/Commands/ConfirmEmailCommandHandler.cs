using MediatR;

namespace MRC.Agendia.Application.Auth.Commands
{
    public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Unit>
    {
        private readonly IAuthService _service;
        public ConfirmEmailCommandHandler(IAuthService service) { _service = service; }
        public async Task<Unit> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            await _service.ConfirmEmailAsync(request.Dto, cancellationToken);
            return Unit.Value;
        }
    }
}
