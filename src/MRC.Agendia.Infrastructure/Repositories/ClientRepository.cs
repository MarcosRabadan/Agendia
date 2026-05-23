using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly AgendiaDbContext _context;

        public ClientRepository(AgendiaDbContext context)
        {
            _context = context;
        }

        public async Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => await _context.Clients.FindAsync(new object?[] { id }, cancellationToken);

        public Task<Client?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
            => _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        public async Task<IEnumerable<Client>> GetAllAsync(CancellationToken cancellationToken = default)
            => await _context.Clients.ToListAsync(cancellationToken);

        public Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => _context.Clients
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
            => await _context.Clients.AddAsync(client, cancellationToken);

        public void Update(Client client)
            => _context.Clients.Update(client);

        public void Delete(Client client)
            => _context.Clients.Remove(client);
    }
}
