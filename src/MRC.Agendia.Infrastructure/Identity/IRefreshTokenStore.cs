namespace MRC.Agendia.Infrastructure.Identity
{
    public interface IRefreshTokenStore
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task AddAsync(RefreshToken refreshToken);
        void Update(RefreshToken refreshToken);
        Task<int> SaveChangesAsync();
    }
}
