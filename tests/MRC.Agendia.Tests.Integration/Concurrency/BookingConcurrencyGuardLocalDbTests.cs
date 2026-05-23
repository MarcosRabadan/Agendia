using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Persistence;

namespace MRC.Agendia.Tests.Integration.Concurrency
{
    /// <summary>
    /// Verifies the booking concurrency guard against a real SQL Server (LocalDB):
    /// two concurrent calls with the same employee+day key must NOT run their
    /// critical sections at the same time (sp_getapplock serializes them).
    ///
    /// Skipped automatically when LocalDB is unreachable (e.g. CI without SQL
    /// Server). The in-memory test store cannot exercise this - it has no shared
    /// database and no application locks - which is exactly why this lives in its
    /// own LocalDB-backed test.
    /// </summary>
    public class BookingConcurrencyGuardLocalDbTests
    {
        private const string LocalDbBase =
            @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;MultipleActiveResultSets=true;Connect Timeout=5";

        [SkippableFact]
        public async Task ExecuteSerializedAsync_SameKey_SerializesConcurrentCriticalSections()
        {
            var connectionString = $"{LocalDbBase};Database=agendia-conc-{Guid.NewGuid():N}";

            AgendiaDbContext NewContext() =>
                new(new DbContextOptionsBuilder<AgendiaDbContext>().UseSqlServer(connectionString).Options);

            // Probe LocalDB; skip cleanly if it is not available in this environment.
            var available = false;
            try
            {
                await using var probe = NewContext();
                await probe.Database.EnsureCreatedAsync();
                available = true;
            }
            catch
            {
                // LocalDB not installed / not reachable here.
            }

            Skip.IfNot(available, "LocalDB no disponible; test de concurrencia omitido.");

            try
            {
                var date = new DateOnly(2026, 7, 1);
                var events = new System.Collections.Concurrent.ConcurrentQueue<(string Phase, long Ticks)>();

                async Task RunSection(string tag)
                {
                    await using var ctx = NewContext();
                    var guard = new BookingConcurrencyGuard(ctx);
                    await guard.ExecuteSerializedAsync(employeeId: 1, date, async () =>
                    {
                        events.Enqueue(($"{tag}-start", DateTime.UtcNow.Ticks));
                        await Task.Delay(400);
                        events.Enqueue(($"{tag}-end", DateTime.UtcNow.Ticks));
                    });
                }

                await Task.WhenAll(RunSection("A"), RunSection("B"));

                // With the lock held per (employee, day), one section fully completes
                // before the other starts: ordered phases must be start,end,start,end
                // (never start,start,...). Without the lock they would interleave.
                var phases = events.OrderBy(e => e.Ticks).Select(e => e.Phase.Split('-')[1]).ToList();
                Assert.Equal(new[] { "start", "end", "start", "end" }, phases);
            }
            finally
            {
                try
                {
                    await using var teardown = NewContext();
                    await teardown.Database.EnsureDeletedAsync();
                }
                catch
                {
                    // Best-effort cleanup of the throwaway test database.
                }
            }
        }
    }
}
