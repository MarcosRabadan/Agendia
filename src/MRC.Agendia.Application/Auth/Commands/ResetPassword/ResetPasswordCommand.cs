using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Commands.ResetPassword
{
    public record ResetPasswordCommand(ResetPasswordDto Dto) : IRequest<Unit>;
}
