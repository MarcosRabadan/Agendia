using Microsoft.Extensions.Caching.Memory;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Infrastructure.Caching;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Caching
{
    /// <summary>
    /// The holiday caching decorator (#55) serves repeated per-year reads from cache
    /// and evicts only the affected year on a write.
    /// </summary>
    public class CachingHolidayCalendarRepositoryTests
    {
        private readonly IHolidayCalendarRepository _inner = Substitute.For<IHolidayCalendarRepository>();
        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly CachingHolidayCalendarRepository _sut;

        public CachingHolidayCalendarRepositoryTests()
        {
            _sut = new CachingHolidayCalendarRepository(_inner, _cache);
        }

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
    }
}
