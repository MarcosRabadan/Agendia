using MediatR;

namespace MRC.Agendia.Application.Auth.Commands.Logout
{
    public record LogoutAllCommand(string UserId) : IRequest<Unit>;
}
