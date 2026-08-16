using System.Buffers.Binary;
using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Application.Appointments;

namespace MRC.Agendia.Infrastructure.Persistence
{
    /// <summary>
    /// PostgreSQL implementation: wraps the booking critical section in a
    /// transaction-scoped advisory lock (pg_advisory_xact_lock) keyed by
    /// employee+day, so concurrent bookings for the same employee/day are
    /// serialized and cannot both pass the capacity check (fixes the check-then-act
    /// double-booking race). The lock auto-releases at the end of the transaction
    /// and only contends across the same (employee, day), never globally.
    ///
    /// On non-PostgreSQL providers (the in-memory test store) there is no shared
    /// database to race against, so the action runs directly - this keeps the
    /// existing in-memory test suite behaviour unchanged.
    /// </summary>
    public class BookingConcurrencyGuard : IBookingConcurrencyGuard
    {
        private readonly AgendiaDbContext _context;

        public BookingConcurrencyGuard(AgendiaDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task ExecuteSerializedAsync(Guid employeeId,
                                                 DateOnly date,
                                                 Func<Task> action,
                                                 CancellationToken cancellationToken = default)
            => await ExecuteSerializedAsync(employeeId, date, async () =>
            {
                await action();
                return true;
            }, cancellationToken);

        /// <inheritdoc />
        public async Task<T> ExecuteSerializedAsync<T>(Guid employeeId,
                                                       DateOnly date,
                                                       Func<Task<T>> action,
                                                       CancellationToken cancellationToken = default)
        {
            // Npgsql-specific: the lock is a pg_advisory_xact_lock. The in-memory test store
            // has nothing to serialize. NOTE (R4): any OTHER relational provider would also
            // fall here and run WITHOUT serialization - this guard is Postgres-only by design;
            // a different database would need its own locking primitive, not a silent no-op.
            if (!_context.Database.IsNpgsql())
                return await action();

            // No retry strategy is configured, so a plain transaction is safe (a
            // retry would otherwise re-run the non-idempotent insert).
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Advisory lock keyed by a stable hash of (employeeId, day): serializes
            // callers for the same employee/day only, and releases automatically at the
            // end of the transaction. pg_advisory_xact_lock takes a bigint, so the Guid
            // employee id + day are folded into a 64-bit key. It blocks until acquired
            // (waits its turn) rather than timing out, the desired booking behaviour.
            // A hash collision would only serialize two unrelated employee/days (safe,
            // never a correctness issue).
            var lockKey = ComputeLockKey(employeeId, date);
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({lockKey})",
                cancellationToken);

            var output = await action();

            await transaction.CommitAsync(cancellationToken);
            return output;
        }

        // Deterministic 64-bit key from the employee id and day (FNV-1a). Stable across
        // processes/runs (unlike string.GetHashCode), so the same employee+day always
        // maps to the same advisory-lock key.
        private static long ComputeLockKey(Guid employeeId, DateOnly date)
        {
            Span<byte> buffer = stackalloc byte[20];
            employeeId.TryWriteBytes(buffer);
            var dateKey = date.Year * 10000 + date.Month * 100 + date.Day;
            BinaryPrimitives.WriteInt32LittleEndian(buffer[16..], dateKey);

            const ulong offsetBasis = 14695981039346656037;
            const ulong prime = 1099511628211;
            var hash = offsetBasis;
            foreach (var b in buffer)
            {
                hash ^= b;
                hash *= prime;
            }
            return unchecked((long)hash);
        }
    }
}
