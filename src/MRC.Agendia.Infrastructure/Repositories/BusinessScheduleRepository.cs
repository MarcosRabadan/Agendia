using MRC.Agendia.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
