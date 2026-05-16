using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Commands
{
    public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponseDto>;
}
