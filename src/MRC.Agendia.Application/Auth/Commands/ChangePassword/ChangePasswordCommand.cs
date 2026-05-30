using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Commands.ChangePassword
{
    public record ChangePasswordCommand(string UserId, ChangePasswordDto Dto) : IRequest<Unit>;
}
