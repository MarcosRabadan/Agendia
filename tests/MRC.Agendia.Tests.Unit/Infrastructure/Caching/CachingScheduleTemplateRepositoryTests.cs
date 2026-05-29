using Microsoft.Extensions.Caching.Memory;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Infrastructure.Caching;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Caching
{
    /// <summary>
    /// The template caching decorator (#55) serves repeated per-business reads from
    /// cache, evicts on writes, and answers GetEffectiveTemplate from the cached list.
    /// </summary>
    public class CachingScheduleTemplateRepositoryTests
    {
        private readonly IScheduleTemplateRepository _inner = Substitute.For<IScheduleTemplateRepository>();
        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly CachingScheduleTemplateRepository _sut;

        public CachingScheduleTemplateRepositoryTests()
        {
            _sut = new CachingScheduleTemplateRepository(_inner, _cache);
        }

        [Fact]
        public async Task GetByBusinessId_SegundaLlamada_SirveDesdeCache()
        {
            _inner.GetByBusinessIdAsync(7, Arg.Any<CancellationToken>())
                .Returns(new List<ScheduleTemplate> { new() { Id = 1, BusinessId = 7 } });

            await _sut.GetByBusinessIdAsync(7);
            await _sut.GetByBusinessIdAsync(7);

            await _inner.Received(1).GetByBusinessIdAsync(7, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Update_EvictaLaCacheDelNegocio()
        {
            _inner.GetByBusinessIdAsync(7, Arg.Any<CancellationToken>())
                .Returns(new List<ScheduleTemplate> { new() { Id = 1, BusinessId = 7 } });

            await _sut.GetByBusinessIdAsync(7);                            // caches
            _sut.Update(new ScheduleTemplate { Id = 1, BusinessId = 7 }); // evicts business 7
            await _sut.GetByBusinessIdAsync(7);                            // re-fetches

            await _inner.Received(2).GetByBusinessIdAsync(7, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetEffectiveTemplate_SeSirveDesdeLaListaCacheada()
        {
            var template = new ScheduleTemplate
            {
                Id = 1,
                BusinessId = 7,
                EffectiveFrom = new DateOnly(2030, 1, 1),
                EffectiveTo = new DateOnly(2030, 12, 31),
                IsDefault = true
            };
            _inner.GetByBusinessIdAsync(7, Arg.Any<CancellationToken>())
                .Returns(new List<ScheduleTemplate> { template });

            await _sut.GetByBusinessIdAsync(7); // caches the per-business list
            var effective = await _sut.GetEffectiveTemplateAsync(7, new DateOnly(2030, 6, 1));

            Assert.NotNull(effective);
            Assert.Equal(1, effective!.Id);
            // Answered from the cached list, not a separate DB query.
            await _inner.DidNotReceive().GetEffectiveTemplateAsync(Arg.Any<int>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
        }
    }
}
