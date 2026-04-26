using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class HolidayCalendarRepository : IHolidayCalendarRepository
    {
        private readonly AgendiaDbContext _context;

        public HolidayCalendarRepository(AgendiaDbContext context)
        {
            _context = context;
        }

        public async Task<HolidayCalendar?> GetByIdAsync(int id)
            => await _context.HolidayCalendars.FindAsync(id);

        public async Task<IEnumerable<HolidayCalendar>> GetAllAsync()
            => await _context.HolidayCalendars.OrderBy(h => h.Date).ToListAsync();

        public async Task<IEnumerable<HolidayCalendar>> GetByYearAsync(int year)
            => await _context.HolidayCalendars
                .Where(h => h.Year == year)
                .OrderBy(h => h.Date)
                .ToListAsync();

        public async Task<IEnumerable<HolidayCalendar>> GetByDateRangeAsync(DateOnly from, DateOnly to)
            => await _context.HolidayCalendars
                .Where(h => h.Date >= from && h.Date <= to)
                .OrderBy(h => h.Date)
                .ToListAsync();

        public async Task AddAsync(HolidayCalendar holiday)
            => await _context.HolidayCalendars.AddAsync(holiday);

        public async Task AddRangeAsync(IEnumerable<HolidayCalendar> holidays)
            => await _context.HolidayCalendars.AddRangeAsync(holidays);

        public void Update(HolidayCalendar holiday)
            => _context.HolidayCalendars.Update(holiday);

        public void Delete(HolidayCalendar holiday)
            => _context.HolidayCalendars.Remove(holiday);
    }
}
