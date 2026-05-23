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

        public async Task<Service?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => await _context.Services.FindAsync(new object?[] { id }, cancellationToken);

        public async Task<IEnumerable<Service>> GetAllAsync(CancellationToken cancellationToken = default)
            => await _context.Services.ToListAsync(cancellationToken);

        public Task<(IReadOnlyList<Service> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => _context.Services
                .AsNoTracking()
                .OrderBy(s => s.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public async Task AddAsync(Service service, CancellationToken cancellationToken = default)
            => await _context.Services.AddAsync(service, cancellationToken);

        public void Update(Service service)
            => _context.Services.Update(service);

        public void Delete(Service service)
            => _context.Services.Remove(service);
    }
}
