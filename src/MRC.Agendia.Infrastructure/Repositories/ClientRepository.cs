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
    }
}
