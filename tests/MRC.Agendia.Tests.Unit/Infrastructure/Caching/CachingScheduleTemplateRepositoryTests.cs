using Microsoft.Extensions.Caching.Memory;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Infrastructure.Caching;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Caching
{
    /// <summary>
    /// The template caching decorator (#55) serves repeated per-business reads from cache,
    /// queues an eviction on writes that lands when the unit of work commits (#306), and
    /// answers GetEffectiveTemplate from the cached list.
    /// </summary>
    public class CachingScheduleTemplateRepositoryTests
    {
        private readonly IScheduleTemplateRepository _inner = Substitute.For<IScheduleTemplateRepository>();
        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly PendingCacheInvalidations _pending;
        private readonly CachingScheduleTemplateRepository _sut;

        public CachingScheduleTemplateRepositoryTests()
        {
            _pending = new PendingCacheInvalidations(_cache);
            _sut = new CachingScheduleTemplateRepository(_inner, _cache, _pending);
        }

        [Fact]
        public async Task GetByBusinessId_SegundaLlamada_SirveDesdeCache()
        {
            _inner.GetByBusinessIdAsync(TestIds.Of(7), Arg.Any<CancellationToken>())
                .Returns(new List<ScheduleTemplate> { new() { Id = TestIds.Of(1), BusinessId = TestIds.Of(7) } });

            await _sut.GetByBusinessIdAsync(TestIds.Of(7));
            await _sut.GetByBusinessIdAsync(TestIds.Of(7));

            await _inner.Received(1).GetByBusinessIdAsync(TestIds.Of(7), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task A_write_does_not_evict_before_the_unit_of_work_commits()
        {
            // #306: evicting at write time - before SaveChanges - let a concurrent reader
            // miss, load the pre-write rows and re-cache them, with nothing invalidating
            // again after the commit. The eviction has to wait for the commit.
            _inner.GetByBusinessIdAsync(TestIds.Of(7), Arg.Any<CancellationToken>())
                .Returns(new List<ScheduleTemplate> { new() { Id = TestIds.Of(1), BusinessId = TestIds.Of(7) } });

            await _sut.GetByBusinessIdAsync(TestIds.Of(7));                                        // caches
            _sut.Update(new ScheduleTemplate { Id = TestIds.Of(1), BusinessId = TestIds.Of(7) });  // queues only

            await _sut.GetByBusinessIdAsync(TestIds.Of(7));

            await _inner.Received(1).GetByBusinessIdAsync(TestIds.Of(7), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task The_commit_flush_evicts_the_business_cache()
        {
            _inner.GetByBusinessIdAsync(TestIds.Of(7), Arg.Any<CancellationToken>())
                .Returns(new List<ScheduleTemplate> { new() { Id = TestIds.Of(1), BusinessId = TestIds.Of(7) } });

            await _sut.GetByBusinessIdAsync(TestIds.Of(7));                                        // caches
            _sut.Update(new ScheduleTemplate { Id = TestIds.Of(1), BusinessId = TestIds.Of(7) });
            _pending.Flush();                                                                      // what UnitOfWork does after committing

            await _sut.GetByBusinessIdAsync(TestIds.Of(7));                                        // re-fetches

            await _inner.Received(2).GetByBusinessIdAsync(TestIds.Of(7), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetEffectiveTemplate_SeSirveDesdeLaListaCacheada()
        {
            var template = new ScheduleTemplate
            {
                Id = TestIds.Of(1),
                BusinessId = TestIds.Of(7),
                EffectiveFrom = new DateOnly(2030, 1, 1),
                EffectiveTo = new DateOnly(2030, 12, 31),
                IsDefault = true
            };
            _inner.GetByBusinessIdAsync(TestIds.Of(7), Arg.Any<CancellationToken>())
                .Returns(new List<ScheduleTemplate> { template });

            await _sut.GetByBusinessIdAsync(TestIds.Of(7)); // caches the per-business list
            var effective = await _sut.GetEffectiveTemplateAsync(TestIds.Of(7), new DateOnly(2030, 6, 1));

            Assert.NotNull(effective);
            Assert.Equal(TestIds.Of(1), effective!.Id);
            // Answered from the cached list, not a separate DB query.
            await _inner.DidNotReceive().GetEffectiveTemplateAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
        }
    }
}
