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

        public async Task<HolidayCalendar?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => await _context.HolidayCalendars.FindAsync(new object?[] { id }, cancellationToken);

        public async Task<IEnumerable<HolidayCalendar>> GetAllAsync(CancellationToken cancellationToken = default)
            => await _context.HolidayCalendars.OrderBy(h => h.Date).ToListAsync(cancellationToken);

        public async Task<IEnumerable<HolidayCalendar>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
            => await _context.HolidayCalendars
                .Where(h => h.Year == year)
                .OrderBy(h => h.Date)
                .ToListAsync(cancellationToken);

        public async Task<IEnumerable<HolidayCalendar>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
            => await _context.HolidayCalendars
                .Where(h => h.Date >= from && h.Date <= to)
                .OrderBy(h => h.Date)
                .ToListAsync(cancellationToken);

        public async Task AddAsync(HolidayCalendar holiday, CancellationToken cancellationToken = default)
            => await _context.HolidayCalendars.AddAsync(holiday, cancellationToken);

        public async Task AddRangeAsync(IEnumerable<HolidayCalendar> holidays, CancellationToken cancellationToken = default)
            => await _context.HolidayCalendars.AddRangeAsync(holidays, cancellationToken);

        public void Update(HolidayCalendar holiday)
            => _context.HolidayCalendars.Update(holiday);

        public void Delete(HolidayCalendar holiday)
            => _context.HolidayCalendars.Remove(holiday);
    }
}
