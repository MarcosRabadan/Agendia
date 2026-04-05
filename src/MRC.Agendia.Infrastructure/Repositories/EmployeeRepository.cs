using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
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

        public async Task<Employee?> GetByIdAsync(int id)
            => await _context.Employees.FindAsync(id);

        public async Task<IEnumerable<Employee>> GetAllAsync()
            => await _context.Employees.ToListAsync();

        public async Task AddAsync(Employee employee)
            => await _context.Employees.AddAsync(employee);

        public void Update(Employee employee)
            => _context.Employees.Update(employee);

        public void Delete(Employee employee)
            => _context.Employees.Remove(employee);
    }
}
