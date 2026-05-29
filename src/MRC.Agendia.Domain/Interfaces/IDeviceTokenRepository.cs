using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IDeviceTokenRepository
    {
        /// <summary>The registration for an exact token, or null. Used to make register idempotent.</summary>
        Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>The push tokens registered for a user (empty if none). Used to fan out a push.</summary>
        Task<IReadOnlyList<string>> GetTokensByUserIdAsync(string userId, CancellationToken cancellationToken = default);

        Task AddAsync(DeviceToken deviceToken, CancellationToken cancellationToken = default);
        void Update(DeviceToken deviceToken);
        void Delete(DeviceToken deviceToken);
    }
}
