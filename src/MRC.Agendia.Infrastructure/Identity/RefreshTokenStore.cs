using Microsoft.EntityFrameworkCore;

namespace MRC.Agendia.Infrastructure.Identity
{
    public class RefreshTokenStore : IRefreshTokenStore
    {
        private readonly AgendiaDbContext _context;

        public RefreshTokenStore(AgendiaDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            // Tokens are stored hashed; hash the presented cleartext value to look it up.
            var hash = RefreshTokenHasher.Hash(token);
            return await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == hash, cancellationToken);
        }

        public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > now)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
            => await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);

        public void Update(RefreshToken refreshToken)
            => _context.RefreshTokens.Update(refreshToken);

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);
    }
}
