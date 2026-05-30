using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IDeviceTokenRepository
    {
        /// <summary>The registration for an exact token, or null. Used to make register idempotent.</summary>
        /// <param name="token">The device token value.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The matching registration, or null when not registered.</returns>
        Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>The push tokens registered for a user (empty if none). Used to fan out a push.</summary>
        /// <param name="userId">Identity user id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The user's registered token values, or an empty list when none.</returns>
        Task<IReadOnlyList<string>> GetTokensByUserIdAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>Adds a new device token registration to the context.</summary>
        /// <param name="deviceToken">The registration to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task AddAsync(DeviceToken deviceToken, CancellationToken cancellationToken = default);

        /// <summary>Marks a device token registration as modified.</summary>
        /// <param name="deviceToken">The registration to update.</param>
        void Update(DeviceToken deviceToken);

        /// <summary>Removes a device token registration from the context.</summary>
        /// <param name="deviceToken">The registration to delete.</param>
        void Delete(DeviceToken deviceToken);
    }
}
