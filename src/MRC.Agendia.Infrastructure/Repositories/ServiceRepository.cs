using MRC.Agendia.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
