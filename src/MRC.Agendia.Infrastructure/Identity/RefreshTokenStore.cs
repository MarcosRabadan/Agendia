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

        public async Task<RefreshToken?> GetByTokenAsync(string token)
            => await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);

        public async Task AddAsync(RefreshToken refreshToken)
            => await _context.RefreshTokens.AddAsync(refreshToken);

        public void Update(RefreshToken refreshToken)
            => _context.RefreshTokens.Update(refreshToken);

        public async Task<int> SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}
