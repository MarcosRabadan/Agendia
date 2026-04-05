using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class BusinessScheduleRepository : IBusinessScheduleRepository
    {
        private readonly AgendiaDbContext _context;

        public BusinessScheduleRepository(AgendiaDbContext context)
        {
            _context = context;
        }
    }
}
