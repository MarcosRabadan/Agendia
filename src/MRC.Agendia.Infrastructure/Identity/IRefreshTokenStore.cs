namespace MRC.Agendia.Infrastructure.Identity
{
    public interface IRefreshTokenStore
    {
        /// <summary>Looks up a refresh token by its cleartext value (hashed internally before the lookup).</summary>
        /// <param name="token">The cleartext refresh token to find.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The matching refresh token, or null if none exists.</returns>
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>Returns the user's active (not revoked, not expired) refresh tokens.</summary>
        /// <param name="userId">The id of the user whose tokens are listed.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The user's currently active refresh tokens.</returns>
        Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>Stages a new refresh token for insertion (not persisted until <see cref="SaveChangesAsync"/>).</summary>
        /// <param name="refreshToken">The refresh token to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

        /// <summary>Marks a tracked refresh token as modified (not persisted until <see cref="SaveChangesAsync"/>).</summary>
        /// <param name="refreshToken">The refresh token to update.</param>
        void Update(RefreshToken refreshToken);

        /// <summary>Persists the pending refresh-token changes to the database.</summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The number of state entries written to the database.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
