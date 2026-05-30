using MediatR;

namespace MRC.Agendia.Application.Auth.Commands.Logout
{
    public record LogoutCommand(string RefreshToken, string UserId) : IRequest<Unit>;
}
