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
    }
}
