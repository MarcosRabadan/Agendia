using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class HolidayCalendarRepository : RepositoryBase<HolidayCalendar>, IHolidayCalendarRepository
    {
        public HolidayCalendarRepository(AgendiaDbContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public async Task<IEnumerable<HolidayCalendar>> GetAllAsync(CancellationToken cancellationToken = default)
            => await Set.AsNoTracking().OrderBy(h => h.Date).ToListAsync(cancellationToken);

        /// <inheritdoc />
        public async Task<IEnumerable<HolidayCalendar>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
            => await Set
                .AsNoTracking()
                .Where(h => h.Year == year)
                .OrderBy(h => h.Date)
                .ToListAsync(cancellationToken);

        /// <inheritdoc />
        public async Task<IEnumerable<HolidayCalendar>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
            => await Set
                .AsNoTracking()
                .Where(h => h.Date >= from && h.Date <= to)
                .OrderBy(h => h.Date)
                .ToListAsync(cancellationToken);

        /// <inheritdoc />
        public async Task AddRangeAsync(IEnumerable<HolidayCalendar> holidays, CancellationToken cancellationToken = default)
            => await Set.AddRangeAsync(holidays, cancellationToken);
    }
}
