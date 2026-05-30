using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Commands.Login
{
    public record LoginCommand(LoginDto Dto) : IRequest<AuthResponseDto>;
}
