using Microsoft.Extensions.Caching.Memory;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Caching
{
    /// <summary>
    /// Caching decorator over <see cref="IScheduleTemplateRepository"/> (#55).
    /// Templates for a business change rarely but the calendar/availability paths
    /// read them on every request, so the per-business template list (AsNoTracking,
    /// detached) is cached for a short TTL and evicted on any write.
    /// <see cref="GetEffectiveTemplateAsync"/> is served from that same cached list.
    ///
    /// Eviction happens at write time (before SaveChanges); on rollback the worst
    /// case is an extra cache miss. The TTL bounds any staleness from a concurrent
    /// reader re-caching during the brief write window.
    /// </summary>
    public class CachingScheduleTemplateRepository : IScheduleTemplateRepository
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

        private readonly IScheduleTemplateRepository _inner;
        private readonly IMemoryCache _cache;

        public CachingScheduleTemplateRepository(IScheduleTemplateRepository inner, IMemoryCache cache)
        {
            _inner = inner;
            _cache = cache;
        }

        private static string Key(Guid businessId) => $"sched-templates:{businessId}";

        /// <inheritdoc />
        public async Task<IEnumerable<ScheduleTemplate>> GetByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
            => await GetCachedByBusinessAsync(businessId, cancellationToken);

        /// <inheritdoc />
        public async Task<ScheduleTemplate?> GetEffectiveTemplateAsync(Guid businessId, DateOnly date, CancellationToken cancellationToken = default)
        {
            // Same selection rule as ScheduleResolver.SelectTemplate, served from cache.
            var templates = await GetCachedByBusinessAsync(businessId, cancellationToken);
            return templates
                .Where(t => t.EffectiveFrom <= date && t.EffectiveTo >= date)
                .OrderByDescending(t => t.IsDefault)
                .FirstOrDefault();
        }

        private async Task<IReadOnlyList<ScheduleTemplate>> GetCachedByBusinessAsync(Guid businessId, CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(Key(businessId), out IReadOnlyList<ScheduleTemplate>? cached) && cached is not null)
                return cached;

            var templates = (await _inner.GetByBusinessIdAsync(businessId, cancellationToken)).ToList();
            _cache.Set(Key(businessId), (IReadOnlyList<ScheduleTemplate>)templates, Ttl);
            return templates;
        }

        // ----- Writes: evict the business's cached templates -----

        /// <inheritdoc />
        public async Task AddAsync(ScheduleTemplate template, CancellationToken cancellationToken = default)
        {
            await _inner.AddAsync(template, cancellationToken);
            _cache.Remove(Key(template.BusinessId));
        }

        /// <inheritdoc />
        public void Update(ScheduleTemplate template)
        {
            _inner.Update(template);
            _cache.Remove(Key(template.BusinessId));
        }

        /// <inheritdoc />
        public void Delete(ScheduleTemplate template)
        {
            _inner.Delete(template);
            _cache.Remove(Key(template.BusinessId));
        }

        // ----- Pass-through (not cached) -----

        /// <inheritdoc />
        public Task<ScheduleTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => _inner.GetByIdAsync(id, cancellationToken);

        /// <inheritdoc />
        public Task<ScheduleTemplate?> GetByIdWithSlotsAsync(Guid id, CancellationToken cancellationToken = default)
            => _inner.GetByIdWithSlotsAsync(id, cancellationToken);

        /// <inheritdoc />
        public Task<bool> HasOverlappingTemplateAsync(Guid businessId,
                                                      DateOnly from,
                                                      DateOnly to,
                                                      Guid? excludeId = null,
                                                      CancellationToken cancellationToken = default)
            => _inner.HasOverlappingTemplateAsync(businessId, from, to, excludeId, cancellationToken);
    }
}
