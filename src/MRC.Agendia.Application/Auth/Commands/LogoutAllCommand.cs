using MediatR;

namespace MRC.Agendia.Application.Auth.Commands
{
    public record LogoutAllCommand(string UserId) : IRequest<Unit>;
}
