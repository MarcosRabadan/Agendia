using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Domain.Entities
{
    /// <summary>
    /// A push-notification device token registered by a user, mapping a physical
    /// device (the token) to the user it should notify. Not soft-deletable: tokens
    /// are hard-removed when the device unregisters or the token rotates.
    /// </summary>
    public class DeviceToken
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string Token { get; set; } = null!;
        public DevicePlatform Platform { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
