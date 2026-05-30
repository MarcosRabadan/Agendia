using MediatR;
using MRC.Agendia.Application.DeviceTokens.DTO;

namespace MRC.Agendia.Application.DeviceTokens.Commands.Register
{
    /// <summary>Registers (or re-points) the caller's push device token.</summary>
    public record RegisterDeviceTokenCommand(RegisterDeviceTokenDto Dto) : IRequest<bool>;
}
