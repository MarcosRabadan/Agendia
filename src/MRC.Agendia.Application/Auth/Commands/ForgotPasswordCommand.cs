using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Commands
{
    public record ForgotPasswordCommand(ForgotPasswordDto Dto) : IRequest<Unit>;
}
