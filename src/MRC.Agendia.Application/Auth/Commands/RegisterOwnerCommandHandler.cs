using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Commands
{
    public class RegisterOwnerCommandHandler : IRequestHandler<RegisterOwnerCommand, AuthResponseDto>
    {
        private readonly IAuthService _service;
        public RegisterOwnerCommandHandler(IAuthService service) { _service = service; }
        public Task<AuthResponseDto> Handle(RegisterOwnerCommand request, CancellationToken cancellationToken)
            => _service.RegisterOwnerAsync(request.Dto);
    }
}
