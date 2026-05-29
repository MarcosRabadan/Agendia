using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.DeviceTokens.DTO
{
    public record RegisterDeviceTokenDto(string Token, DevicePlatform Platform);
}
