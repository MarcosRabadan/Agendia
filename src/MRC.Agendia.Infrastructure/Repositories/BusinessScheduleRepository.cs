using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
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

        public async Task<BusinessSchedule?> GetByIdAsync(int id)
            => await _context.BusinessSchedules.FindAsync(id);

        public async Task<IEnumerable<BusinessSchedule>> GetAllAsync()
            => await _context.BusinessSchedules.ToListAsync();

        public async Task AddAsync(BusinessSchedule businessSchedule)
            => await _context.BusinessSchedules.AddAsync(businessSchedule);

        public void Update(BusinessSchedule businessSchedule)
            => _context.BusinessSchedules.Update(businessSchedule);

        public void Delete(BusinessSchedule businessSchedule)
            => _context.BusinessSchedules.Remove(businessSchedule);
    }
}
