using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Idempotency;

namespace MRC.Agendia.Infrastructure.Idempotency
{
    /// <summary>
    /// EF Core implementation of the idempotency record. The claim is an INSERT guarded
    /// by the primary key: under a real race both requests try to insert, the database
    /// lets exactly one through and the loser reads the winner's row - the same
    /// check-then-act problem the booking guard solves with an advisory lock, solved
    /// here by the key itself.
    ///
    /// <para>Every operation runs on its OWN DI scope, so the bookkeeping never shares a
    /// change tracker with the request it is guarding: saving the claim (or deleting it
    /// after a rejected request) must not flush the half-built entities the handler may
    /// be tracking at that moment.</para>
    /// </summary>
    public class IdempotencyStore : IIdempotencyStore
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public IdempotencyStore(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        /// <inheritdoc />
        public async Task<IdempotencyClaim> TryClaimAsync(string key,
                                                          string requestHash,
                                                          CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

            var existing = await FindAsync(context, key, cancellationToken);
            if (existing is not null)
                return Describe(existing, requestHash);

            context.IdempotencyRecords.Add(new IdempotencyRecord
            {
                Key = key,
                RequestHash = requestHash,
                CreatedAtUtc = DateTime.UtcNow
            });

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return new IdempotencyClaim(IdempotencyClaimOutcome.Claimed);
            }
            catch (DbUpdateException)
            {
                // Lost the race: a twin inserted the same key between the read and the
                // write. Answer from the winner's row; if there is none the insert failed
                // for some other reason, and that must surface rather than let the same
                // request be served twice.
                using var readScope = _scopeFactory.CreateScope();
                var readContext = readScope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

                var winner = await FindAsync(readContext, key, cancellationToken);
                if (winner is null)
                    throw;

                return Describe(winner, requestHash);
            }
        }

        /// <inheritdoc />
        public async Task CompleteAsync(string key,
                                        int statusCode,
                                        string responseBody,
                                        CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

            var record = await context.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.Key == key, cancellationToken);
            if (record is null)
                return;

            record.StatusCode = statusCode;
            record.ResponseBody = responseBody;
            record.CompletedAtUtc = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task ReleaseAsync(string key, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

            // Only an in-flight claim is dropped: a completed one is the answer future
            // retries replay.
            var record = await context.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.Key == key && r.CompletedAtUtc == null, cancellationToken);
            if (record is null)
                return;

            context.IdempotencyRecords.Remove(record);
            await context.SaveChangesAsync(cancellationToken);
        }

        private static Task<IdempotencyRecord?> FindAsync(AgendiaDbContext context,
                                                          string key,
                                                          CancellationToken cancellationToken)
            => context.IdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Key == key, cancellationToken);

        private static IdempotencyClaim Describe(IdempotencyRecord record, string requestHash)
        {
            if (record.RequestHash != requestHash)
                return new IdempotencyClaim(IdempotencyClaimOutcome.KeyReused);

            return record.CompletedAtUtc is null
                ? new IdempotencyClaim(IdempotencyClaimOutcome.InProgress)
                : new IdempotencyClaim(IdempotencyClaimOutcome.Replay,
                                       record.StatusCode,
                                       record.ResponseBody);
        }
    }
}
