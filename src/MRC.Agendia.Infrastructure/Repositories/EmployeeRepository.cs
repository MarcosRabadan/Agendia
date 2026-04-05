
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly AgendiaDbContext _context;

        public EmployeeRepository(AgendiaDbContext context)
        {
            _context = context;
        }
    }
}
