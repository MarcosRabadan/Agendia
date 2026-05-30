using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Commands.ConfirmEmail
{
    public record ConfirmEmailCommand(ConfirmEmailDto Dto) : IRequest<Unit>;
}
