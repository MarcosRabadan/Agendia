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
    }
}
