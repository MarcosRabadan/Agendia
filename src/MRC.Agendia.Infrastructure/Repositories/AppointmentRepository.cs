using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly AgendiaDbContext _context;
        public AppointmentRepository(AgendiaDbContext context)
        {
            _context = context;
        }

    }
}
