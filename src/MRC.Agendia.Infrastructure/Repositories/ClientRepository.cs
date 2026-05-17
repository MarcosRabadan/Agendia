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

        public async Task<Client?> GetByIdAsync(int id)
            => await _context.Clients.FindAsync(id);

        public async Task<IEnumerable<Client>> GetAllAsync()
            => await _context.Clients.ToListAsync();

        public Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => _context.Clients
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public async Task AddAsync(Client client)
            => await _context.Clients.AddAsync(client);

        public void Update(Client client)
            => _context.Clients.Update(client);

        public void Delete(Client client)
            => _context.Clients.Remove(client);
    }
}
