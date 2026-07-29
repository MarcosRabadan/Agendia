using MediatR;
using MRC.Agendia.Application.ServiceAuth.DTO;

namespace MRC.Agendia.Application.ServiceAuth.Commands
{
    /// <summary>
    /// Authenticates a trusted service by its client credentials and issues a
    /// short-lived service token.
    /// </summary>
    public record AuthenticateServiceCommand(ServiceTokenRequestDto Dto) : IRequest<ServiceTokenResponseDto>;
}
