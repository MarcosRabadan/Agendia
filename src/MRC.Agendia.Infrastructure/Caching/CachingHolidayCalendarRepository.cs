using Microsoft.Extensions.Caching.Memory;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Caching
{
    /// <summary>
    /// Caching decorator over <see cref="IHolidayCalendarRepository"/> (#55).
    /// Holidays for a year are effectively immutable, so the per-year list
    /// (AsNoTracking, detached) is cached and evicted on a write of that year.
    /// Each <see cref="HolidayCalendar"/> carries its <c>Year</c>, so a write
    /// evicts exactly the affected year(s) - no shared key-tracking needed.
    /// </summary>
    public class CachingHolidayCalendarRepository : IHolidayCalendarRepository
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);

        private readonly IHolidayCalendarRepository _inner;
        private readonly IMemoryCache _cache;

        public CachingHolidayCalendarRepository(IHolidayCalendarRepository inner, IMemoryCache cache)
        {
            _inner = inner;
            _cache = cache;
        }

        private static string Key(int year) => $"holidays:{year}";

        public async Task<IEnumerable<HolidayCalendar>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(Key(year), out IReadOnlyList<HolidayCalendar>? cached) && cached is not null)
                return cached;

            var holidays = (await _inner.GetByYearAsync(year, cancellationToken)).ToList();
            _cache.Set(Key(year), (IReadOnlyList<HolidayCalendar>)holidays, Ttl);
            return holidays;
        }

        // ----- Writes: evict exactly the affected year(s) -----

        public async Task AddAsync(HolidayCalendar holiday, CancellationToken cancellationToken = default)
        {
            await _inner.AddAsync(holiday, cancellationToken);
            _cache.Remove(Key(holiday.Year));
        }

        public async Task AddRangeAsync(IEnumerable<HolidayCalendar> holidays, CancellationToken cancellationToken = default)
        {
            var list = holidays as IReadOnlyCollection<HolidayCalendar> ?? holidays.ToList();
            await _inner.AddRangeAsync(list, cancellationToken);
            foreach (var year in list.Select(h => h.Year).Distinct())
                _cache.Remove(Key(year));
        }

        public void Update(HolidayCalendar holiday)
        {
            _inner.Update(holiday);
            _cache.Remove(Key(holiday.Year));
        }

        public void Delete(HolidayCalendar holiday)
        {
            _inner.Delete(holiday);
            _cache.Remove(Key(holiday.Year));
        }

        // ----- Pass-through (not cached) -----

        public Task<HolidayCalendar?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => _inner.GetByIdAsync(id, cancellationToken);

        public Task<IEnumerable<HolidayCalendar>> GetAllAsync(CancellationToken cancellationToken = default)
            => _inner.GetAllAsync(cancellationToken);

        public Task<IEnumerable<HolidayCalendar>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
            => _inner.GetByDateRangeAsync(from, to, cancellationToken);
    }
}
