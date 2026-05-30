using MediatR;
using MRC.Agendia.Application.Auth.DTO;

namespace MRC.Agendia.Application.Auth.Commands.Registration
{
    public record RegisterClientCommand(RegisterClientDto Dto) : IRequest<AuthResponseDto>;
}
