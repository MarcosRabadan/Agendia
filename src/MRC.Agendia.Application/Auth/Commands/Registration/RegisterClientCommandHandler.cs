using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Commands.Registration
{
    public class RegisterClientCommandHandler : IRequestHandler<RegisterClientCommand, AuthResponseDto>
    {
        private readonly IUserRegistrationService _service;
        public RegisterClientCommandHandler(IUserRegistrationService service) { _service = service; }
        public Task<AuthResponseDto> Handle(RegisterClientCommand request, CancellationToken cancellationToken)
            => _service.RegisterClientAsync(request.Dto, cancellationToken);
    }
}
