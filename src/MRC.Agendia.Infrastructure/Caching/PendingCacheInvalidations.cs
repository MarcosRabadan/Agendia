using Microsoft.Extensions.Caching.Memory;

namespace MRC.Agendia.Infrastructure.Caching
{
    /// <summary>
    /// Cache keys whose entries must be dropped once the current unit of work COMMITS (#306).
    ///
    /// <para>The caching decorators used to evict the moment they were called, which is before
    /// the write ever reaches the database - <c>AddAsync</c> only enqueues in the change
    /// tracker. A concurrent reader could then miss, load the OLD rows, and re-populate the
    /// cache with them; the commit landed afterwards and nothing invalidated again. The stale
    /// schedule was served for the rest of the TTL (five minutes for templates, thirty for
    /// holidays) while <c>IScheduleResolver</c> feeds both availability and the booking
    /// validator, so the API could publish an old calendar and refuse bookings that were by
    /// then perfectly valid.</para>
    ///
    /// <para>Scoped: one instance per request / unit of work. <see cref="UnitOfWork"/> flushes
    /// it after a successful save, or after the commit when the save runs inside a transaction.
    /// Evicting a little late is harmless - readers see data that is still current - whereas
    /// evicting early was precisely the bug.</para>
    /// </summary>
    public class PendingCacheInvalidations
    {
        private readonly IMemoryCache _cache;
        private readonly HashSet<string> _keys = new();

        public PendingCacheInvalidations(IMemoryCache cache)
        {
            _cache = cache;
        }

        /// <summary>Queues a cache key to be evicted when the unit of work commits.</summary>
        /// <param name="key">Cache key to drop.</param>
        public void Add(string key) => _keys.Add(key);

        /// <summary>Evicts every queued key and empties the queue.</summary>
        public void Flush()
        {
            if (_keys.Count == 0)
                return;

            foreach (var key in _keys)
                _cache.Remove(key);

            _keys.Clear();
        }
    }
}
