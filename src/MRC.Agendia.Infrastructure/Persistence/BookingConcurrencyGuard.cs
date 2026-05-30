using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Application.Appointments;

namespace MRC.Agendia.Infrastructure.Persistence
{
    /// <summary>
    /// SQL Server implementation: wraps the booking critical section in a
    /// transaction-scoped application lock (sp_getapplock) keyed by employee+day,
    /// so concurrent bookings for the same employee/day are serialized and cannot
    /// both pass the capacity check (fixes the check-then-act double-booking race).
    /// The lock auto-releases on commit/rollback and only contends across the same
    /// (employee, day), never globally.
    ///
    /// On non-SQL-Server providers (the in-memory test store) there is no shared
    /// database to race against, so the action runs directly - this keeps the
    /// existing in-memory test suite behaviour unchanged.
    /// </summary>
    public class BookingConcurrencyGuard : IBookingConcurrencyGuard
    {
        private const int LockTimeoutMs = 10_000;

        private readonly AgendiaDbContext _context;

        public BookingConcurrencyGuard(AgendiaDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task ExecuteSerializedAsync(
            int employeeId,
            DateOnly date,
            Func<Task> action,
            CancellationToken cancellationToken = default)
            => await ExecuteSerializedAsync(employeeId, date, async () =>
            {
                await action();
                return true;
            }, cancellationToken);

        /// <inheritdoc />
        public async Task<T> ExecuteSerializedAsync<T>(
            int employeeId,
            DateOnly date,
            Func<Task<T>> action,
            CancellationToken cancellationToken = default)
        {
            if (!_context.Database.IsSqlServer())
                return await action();

            // No retry strategy is configured, so a plain transaction is safe (a
            // retry would otherwise re-run the non-idempotent insert).
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var resource = $"booking:{employeeId}:{date:yyyy-MM-dd}";

            // Exclusive, transaction-owned application lock: serializes callers for
            // the same employee/day and releases automatically on commit. THROW if
            // it cannot be acquired within the timeout (only under heavy contention).
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $@"DECLARE @result int;
EXEC @result = sp_getapplock @Resource = {resource}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = {LockTimeoutMs};
IF @result < 0 THROW 51000, 'No se pudo adquirir el lock de reserva.', 1;",
                cancellationToken);

            var output = await action();

            await transaction.CommitAsync(cancellationToken);
            return output;
        }
    }
}
