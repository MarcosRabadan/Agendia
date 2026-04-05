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

        public async Task<Business?> GetByIdAsync(int id)
            => await _context.Businesses.FindAsync(id);

        public async Task<IEnumerable<Business>> GetAllAsync()
            => await _context.Businesses.ToListAsync();

        public async Task AddAsync(Business business)
            => await _context.Businesses.AddAsync(business);

        public void Update(Business business)
            => _context.Businesses.Update(business);

        public void Delete(Business business)
            => _context.Businesses.Remove(business);
    }
}
