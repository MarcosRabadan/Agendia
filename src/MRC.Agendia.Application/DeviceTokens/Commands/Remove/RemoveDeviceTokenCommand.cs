using MediatR;
using MRC.Agendia.Application.DeviceTokens.DTO;

namespace MRC.Agendia.Application.DeviceTokens.Commands.Remove
{
    /// <summary>Removes the caller's push device token (idempotent).</summary>
    public record RemoveDeviceTokenCommand(RemoveDeviceTokenDto Dto) : IRequest<bool>;
}
