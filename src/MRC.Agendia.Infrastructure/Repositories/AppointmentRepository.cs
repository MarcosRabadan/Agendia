using MRC.Agendia.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
