using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class BusinessRepository : IBusinessRepository
    {
        private readonly AgendiaDbContext _context;

        public BusinessRepository(AgendiaDbContext context)
        {
            _context = context;
        }

        public async Task<Business?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => await _context.Businesses.FindAsync(new object?[] { id }, cancellationToken);

        public Task<Business?> GetActiveByIdAsync(int id, CancellationToken cancellationToken = default)
            => _context.Businesses
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id && b.IsActive, cancellationToken);

        public async Task<IEnumerable<Business>> GetAllAsync(CancellationToken cancellationToken = default)
            => await _context.Businesses.ToListAsync(cancellationToken);

        public Task<(IReadOnlyList<Business> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => _context.Businesses
                .AsNoTracking()
                .OrderBy(b => b.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public Task<(IReadOnlyList<Business> Items, int TotalCount)> GetPagedActiveAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => _context.Businesses
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public async Task AddAsync(Business business, CancellationToken cancellationToken = default)
            => await _context.Businesses.AddAsync(business, cancellationToken);

        public void Update(Business business)
            => _context.Businesses.Update(business);

        public void Delete(Business business)
            => _context.Businesses.Remove(business);
    }
}
