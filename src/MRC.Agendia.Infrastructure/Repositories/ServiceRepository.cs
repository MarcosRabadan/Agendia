using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class ServiceRepository : IServiceRepository
    {
        private readonly AgendiaDbContext _context;

        public ServiceRepository(AgendiaDbContext context)
        {
            _context = context;
        }

        public async Task<Service?> GetByIdAsync(int id)
            => await _context.Services.FindAsync(id);

        public async Task<IEnumerable<Service>> GetAllAsync()
            => await _context.Services.ToListAsync();

        public Task<(IReadOnlyList<Service> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => _context.Services
                .AsNoTracking()
                .OrderBy(s => s.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public async Task AddAsync(Service service)
            => await _context.Services.AddAsync(service);

        public void Update(Service service)
            => _context.Services.Update(service);

        public void Delete(Service service)
            => _context.Services.Remove(service);
    }
}
