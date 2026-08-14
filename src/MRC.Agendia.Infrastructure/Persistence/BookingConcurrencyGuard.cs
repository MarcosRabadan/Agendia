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
        public async Task ExecuteSerializedAsync(int employeeId,
                                                 DateOnly date,
                                                 Func<Task> action,
                                                 CancellationToken cancellationToken = default)
            => await ExecuteSerializedAsync(employeeId, date, async () =>
            {
                await action();
                return true;
            }, cancellationToken);

        /// <inheritdoc />
        public async Task<T> ExecuteSerializedAsync<T>(int employeeId,
                                                       DateOnly date,
                                                       Func<Task<T>> action,
                                                       CancellationToken cancellationToken = default)
        {
            if (!_context.Database.IsNpgsql())
                return await action();

            // No retry strategy is configured, so a plain transaction is safe (a
            // retry would otherwise re-run the non-idempotent insert).
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Two-int advisory lock keyed by (employeeId, yyyymmdd): serializes callers
            // for the same employee/day only, and releases automatically at the end of
            // the transaction. It blocks until acquired (waits its turn) rather than
            // timing out, which is the desired behaviour for booking serialization.
            var dateKey = date.Year * 10000 + date.Month * 100 + date.Day;
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({employeeId}, {dateKey})",
                cancellationToken);

            var output = await action();

            await transaction.CommitAsync(cancellationToken);
            return output;
        }
    }
}
