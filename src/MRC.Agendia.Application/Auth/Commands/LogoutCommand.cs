using MediatR;

namespace MRC.Agendia.Application.Auth.Commands
{
    public record LogoutCommand(string RefreshToken, string UserId) : IRequest<Unit>;
}
