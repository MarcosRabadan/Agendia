
using MRC.Agendia.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
