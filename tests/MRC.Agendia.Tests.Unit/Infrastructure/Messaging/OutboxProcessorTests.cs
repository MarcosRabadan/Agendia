using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Messaging;
using MRC.Agendia.Tests.Unit.TestDoubles;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Messaging
{
    /// <summary>
    /// Unit tests for <see cref="OutboxProcessor"/> (in-memory context): delivery marks a
    /// message processed, a failing transport leaves it pending and counts the attempt, a
    /// poison message does not block newer ones, and the purge removes only old processed rows.
    /// </summary>
    public class OutboxProcessorTests
    {
        private static AgendiaDbContext NewContext(string dbName) =>
            new(new DbContextOptionsBuilder<AgendiaDbContext>()
                .UseInMemoryDatabase(dbName)
                .ConfigureWarnings(w => w.Ignore(
                    CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
                .Options, new UnrestrictedBusinessScope());

        private static OutboxProcessor NewProcessor(AgendiaDbContext ctx, IEventTransport transport, OutboxOptions? options = null) =>
            new(ctx, transport, Options.Create(options ?? new OutboxOptions()), NullLogger<OutboxProcessor>.Instance);

        private static OutboxMessage Msg(DateTime occurredOn, int attempts = 0, DateTime? processedOn = null) => new()
        {
            Id = Guid.NewGuid(),
            Type = "AppointmentConfirmed",
            Payload = "{}",
            OccurredOnUtc = occurredOn,
            Attempts = attempts,
            ProcessedOnUtc = processedOn
        };

        [Fact]
        public async Task DispatchPendingAsync_DeliversPending_MarksProcessed()
        {
            await using var ctx = NewContext(nameof(DispatchPendingAsync_DeliversPending_MarksProcessed));
            ctx.Set<OutboxMessage>().Add(Msg(DateTime.UtcNow));
            await ctx.SaveChangesAsync();

            var transport = Substitute.For<IEventTransport>();
            var delivered = await NewProcessor(ctx, transport).DispatchPendingAsync();

            Assert.Equal(1, delivered);
            var stored = await ctx.Set<OutboxMessage>().SingleAsync();
            Assert.NotNull(stored.ProcessedOnUtc);
            Assert.Null(stored.Error);
            await transport.Received(1).PublishAsync("AppointmentConfirmed", "{}", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DispatchPendingAsync_TransportThrows_LeavesPending_AndCountsAttempt()
        {
            await using var ctx = NewContext(nameof(DispatchPendingAsync_TransportThrows_LeavesPending_AndCountsAttempt));
            ctx.Set<OutboxMessage>().Add(Msg(DateTime.UtcNow));
            await ctx.SaveChangesAsync();

            var transport = Substitute.For<IEventTransport>();
            transport.PublishAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(_ => throw new InvalidOperationException("broker down"));

            var delivered = await NewProcessor(ctx, transport).DispatchPendingAsync();

            Assert.Equal(0, delivered);
            var stored = await ctx.Set<OutboxMessage>().SingleAsync();
            Assert.Null(stored.ProcessedOnUtc);   // still pending -> retried next cycle
            Assert.Equal(1, stored.Attempts);
            Assert.Equal("broker down", stored.Error);
        }

        [Fact]
        public async Task DispatchPendingAsync_PoisonMessage_DoesNotBlockNewerMessages()
        {
            await using var ctx = NewContext(nameof(DispatchPendingAsync_PoisonMessage_DoesNotBlockNewerMessages));
            var options = new OutboxOptions { MaxAttempts = 3 };
            // Oldest message has exhausted its retries (poison): it must be skipped so it
            // cannot occupy the batch and starve newer messages.
            var poison = Msg(DateTime.UtcNow.AddMinutes(-10), attempts: 3);
            poison.Error = "always fails";
            // Newer, healthy message that must still be delivered despite the poison ahead of it.
            var healthy = Msg(DateTime.UtcNow);
            ctx.Set<OutboxMessage>().AddRange(poison, healthy);
            await ctx.SaveChangesAsync();

            var transport = Substitute.For<IEventTransport>();
            var delivered = await NewProcessor(ctx, transport, options).DispatchPendingAsync();

            Assert.Equal(1, delivered);
            var poisonStored = await ctx.Set<OutboxMessage>().SingleAsync(m => m.Id == poison.Id);
            var healthyStored = await ctx.Set<OutboxMessage>().SingleAsync(m => m.Id == healthy.Id);
            Assert.Null(poisonStored.ProcessedOnUtc);      // never touched
            Assert.Equal(3, poisonStored.Attempts);
            Assert.NotNull(healthyStored.ProcessedOnUtc);  // delivered
        }

        [Fact]
        public async Task PurgeProcessedAsync_RemovesOldProcessed_KeepsRecentAndPending()
        {
            await using var ctx = NewContext(nameof(PurgeProcessedAsync_RemovesOldProcessed_KeepsRecentAndPending));
            var options = new OutboxOptions { RetentionDays = 7 };
            var oldProcessed = Msg(DateTime.UtcNow.AddDays(-30), processedOn: DateTime.UtcNow.AddDays(-30));
            var recentProcessed = Msg(DateTime.UtcNow.AddDays(-1), processedOn: DateTime.UtcNow.AddDays(-1));
            var oldPending = Msg(DateTime.UtcNow.AddDays(-30)); // old but not processed -> keep
            ctx.Set<OutboxMessage>().AddRange(oldProcessed, recentProcessed, oldPending);
            await ctx.SaveChangesAsync();

            var purged = await NewProcessor(ctx, Substitute.For<IEventTransport>(), options).PurgeProcessedAsync();

            Assert.Equal(1, purged);
            var remaining = await ctx.Set<OutboxMessage>().Select(m => m.Id).ToListAsync();
            Assert.DoesNotContain(oldProcessed.Id, remaining);
            Assert.Contains(recentProcessed.Id, remaining);
            Assert.Contains(oldPending.Id, remaining);
        }
    }
}
