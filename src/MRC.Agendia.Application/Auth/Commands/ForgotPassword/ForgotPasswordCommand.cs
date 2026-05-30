using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Commands.ForgotPassword
{
    public record ForgotPasswordCommand(ForgotPasswordDto Dto) : IRequest<Unit>;
}
