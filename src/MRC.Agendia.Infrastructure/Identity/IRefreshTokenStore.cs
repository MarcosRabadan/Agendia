namespace MRC.Agendia.Infrastructure.Identity
{
    public interface IRefreshTokenStore
    {
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
        void Update(RefreshToken refreshToken);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
