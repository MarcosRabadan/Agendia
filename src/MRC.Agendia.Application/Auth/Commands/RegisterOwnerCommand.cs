using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Commands
{
    public record RegisterOwnerCommand(RegisterOwnerDto Dto) : IRequest<AuthResponseDto>;
}
