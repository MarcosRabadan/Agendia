using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Commands
{
    public class RegisterEmployeeCommandHandler : IRequestHandler<RegisterEmployeeCommand, UserDto>
    {
        private readonly IAuthService _service;
        public RegisterEmployeeCommandHandler(IAuthService service) { _service = service; }
        public Task<UserDto> Handle(RegisterEmployeeCommand request, CancellationToken cancellationToken)
            => _service.RegisterEmployeeAsync(request.Dto, request.CurrentOwnerUserId);
    }
}
