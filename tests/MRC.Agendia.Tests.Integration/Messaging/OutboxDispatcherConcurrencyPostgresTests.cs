using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MRC.Agendia.Infrastructure.Messaging;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Messaging
{
    /// <summary>
    /// Verifies against a REAL PostgreSQL (Testcontainers) that the outbox dispatcher is
    /// N-instance safe: two <see cref="OutboxProcessor"/> instances (each with its own
    /// context/connection, mimicking two replicas) dispatching the SAME table at the same
    /// time deliver each event EXACTLY once. The FOR UPDATE SKIP LOCKED claim is what makes
    /// that hold; the in-memory suite cannot exercise it (no row locks), which is why this
    /// lives in a Postgres-backed test.
    ///
    /// Skipped automatically when Docker is unavailable.
    /// </summary>
    [Collection(PostgresCollection.Name)]
    public class OutboxDispatcherConcurrencyPostgresTests
    {
        private readonly PostgresContainerFixture _postgres;

        public OutboxDispatcherConcurrencyPostgresTests(PostgresContainerFixture postgres)
        {
            _postgres = postgres;
        }

        [SkippableFact]
        public async Task DispatchPendingAsync_ConcurrentInstances_DeliverEachEventExactlyOnce()
        {
            Skip.IfNot(_postgres.Available, "Docker/Postgres no disponible; test de concurrencia omitido.");

            const int total = 6;
            await using (var seed = _postgres.CreateContext())
            {
                // Isolate from other tests sharing the container: start from an empty outbox
                // so the "all delivered / none pending" assertions below cannot be tripped by
                // messages another test left behind.
                await seed.Set<OutboxMessage>().ExecuteDeleteAsync();
                for (var i = 0; i < total; i++)
                    seed.Set<OutboxMessage>().Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        Type = "AppointmentConfirmed",
                        Payload = $"payload-{i}",
                        OccurredOnUtc = DateTime.UtcNow.AddSeconds(i)
                    });
                await seed.SaveChangesAsync();
            }

            var transport = new RecordingTransport();
            var options = Options.Create(new OutboxOptions { BatchSize = total });

            async Task RunInstance()
            {
                await using var ctx = _postgres.CreateContext();
                var processor = new OutboxProcessor(ctx, transport, options, NullLogger<OutboxProcessor>.Instance);
                await processor.DispatchPendingAsync();
            }

            // Two instances dispatch the same table at the same time.
            await Task.WhenAll(RunInstance(), RunInstance());

            // Each event delivered EXACTLY once across both instances: no duplicates (SKIP
            // LOCKED stops the second instance re-picking claimed rows) and none dropped.
            var payloads = transport.DeliveredPayloads.ToList();
            Assert.Equal(total, payloads.Count);
            Assert.Equal(total, payloads.Distinct().Count());

            await using var check = _postgres.CreateContext();
            Assert.Equal(0, await check.Set<OutboxMessage>().CountAsync(m => m.ProcessedOnUtc == null));
        }

        private sealed class RecordingTransport : IEventTransport
        {
            public ConcurrentQueue<string> DeliveredPayloads { get; } = new();

            public Task PublishAsync(string type, string payload, CancellationToken cancellationToken = default)
            {
                DeliveredPayloads.Enqueue(payload);
                return Task.CompletedTask;
            }
        }
    }
}
