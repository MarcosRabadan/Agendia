using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Caching;
using MRC.Agendia.Tests.Unit.TestDoubles;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Caching
{
    /// <summary>
    /// The holiday caching decorator (#55) serves repeated per-year reads from cache
    /// and evicts only the affected year on a write. A cross-year update evicts both
    /// the old and the new year so neither per-year list goes stale.
    /// </summary>
    public class CachingHolidayCalendarRepositoryTests
    {
        private readonly IHolidayCalendarRepository _inner = Substitute.For<IHolidayCalendarRepository>();
        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly AgendiaDbContext _context = NewContext();
        private readonly CachingHolidayCalendarRepository _sut;

        public CachingHolidayCalendarRepositoryTests()
        {
            _sut = new CachingHolidayCalendarRepository(_inner, _cache, _context);
        }

        private static AgendiaDbContext NewContext() =>
            new(new DbContextOptionsBuilder<AgendiaDbContext>()
                .UseInMemoryDatabase($"holidays-cache-{Guid.NewGuid()}")
                .Options, new UnrestrictedBusinessScope());

        [Fact]
        public async Task GetByYear_SegundaLlamada_SirveDesdeCache()
        {
            _inner.GetByYearAsync(2030, Arg.Any<CancellationToken>()).Returns(new List<HolidayCalendar>());

            await _sut.GetByYearAsync(2030);
            await _sut.GetByYearAsync(2030);

            await _inner.Received(1).GetByYearAsync(2030, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Add_SoloEvictaElAnioAfectado()
        {
            _inner.GetByYearAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new List<HolidayCalendar>());
            await _sut.GetByYearAsync(2030); // cache 2030
            await _sut.GetByYearAsync(2031); // cache 2031

            await _sut.AddAsync(new HolidayCalendar { Year = 2030 }); // evicts only 2030

            await _sut.GetByYearAsync(2030); // re-fetches
            await _sut.GetByYearAsync(2031); // still cached

            await _inner.Received(2).GetByYearAsync(2030, Arg.Any<CancellationToken>());
            await _inner.Received(1).GetByYearAsync(2031, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Update_CambiaDeAnio_EvictaAmbosAnios()
        {
            _inner.GetByYearAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new List<HolidayCalendar>());

            // Persist a holiday in 2025 so the change tracker holds its original year.
            var holiday = new HolidayCalendar { Date = new DateOnly(2025, 12, 31), Year = 2025, Name = "Fin de anio" };
            _context.Add(holiday);
            await _context.SaveChangesAsync();

            await _sut.GetByYearAsync(2025); // cache 2025
            await _sut.GetByYearAsync(2026); // cache 2026

            // Move it across the year boundary; the entity now carries the new year.
            holiday.Date = new DateOnly(2026, 1, 1);
            holiday.Year = 2026;
            _sut.Update(holiday); // must evict BOTH 2025 (old) and 2026 (new)

            await _sut.GetByYearAsync(2025); // re-fetches (old year was evicted)
            await _sut.GetByYearAsync(2026); // re-fetches (new year was evicted)

            await _inner.Received(2).GetByYearAsync(2025, Arg.Any<CancellationToken>());
            await _inner.Received(2).GetByYearAsync(2026, Arg.Any<CancellationToken>());
        }
    }
}
