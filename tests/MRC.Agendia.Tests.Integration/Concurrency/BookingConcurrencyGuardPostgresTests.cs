using MRC.Agendia.Infrastructure.Persistence;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Concurrency
{
    /// <summary>
    /// Verifies the booking concurrency guard against a real PostgreSQL (Testcontainers):
    /// two concurrent calls with the same employee+day key must NOT run their critical
    /// sections at the same time (pg_advisory_xact_lock serializes them).
    ///
    /// Skipped automatically when Docker is unavailable. The in-memory test store cannot
    /// exercise this - it has no shared database and no advisory locks - which is exactly
    /// why this lives in its own Postgres-backed test.
    /// </summary>
    [Collection(PostgresCollection.Name)]
    public class BookingConcurrencyGuardPostgresTests
    {
        private readonly PostgresContainerFixture _postgres;

        public BookingConcurrencyGuardPostgresTests(PostgresContainerFixture postgres)
        {
            _postgres = postgres;
        }

        [SkippableFact]
        public async Task ExecuteSerializedAsync_SameKey_SerializesConcurrentCriticalSections()
        {
            Skip.IfNot(_postgres.Available, "Docker/Postgres no disponible; test de concurrencia omitido.");

            var date = new DateOnly(2026, 7, 1);
            var events = new System.Collections.Concurrent.ConcurrentQueue<(string Phase, long Ticks)>();

            async Task RunSection(string tag)
            {
                await using var ctx = _postgres.CreateContext();
                var guard = new BookingConcurrencyGuard(ctx);
                await guard.ExecuteSerializedAsync(employeeId: 1, date, async () =>
                {
                    events.Enqueue(($"{tag}-start", DateTime.UtcNow.Ticks));
                    await Task.Delay(400);
                    events.Enqueue(($"{tag}-end", DateTime.UtcNow.Ticks));
                });
            }

            await Task.WhenAll(RunSection("A"), RunSection("B"));

            // With the lock held per (employee, day), one section fully completes before
            // the other starts: ordered phases must be start,end,start,end (never
            // start,start,...). Without the lock they would interleave.
            var phases = events.OrderBy(e => e.Ticks).Select(e => e.Phase.Split('-')[1]).ToList();
            Assert.Equal(new[] { "start", "end", "start", "end" }, phases);
        }
    }
}
