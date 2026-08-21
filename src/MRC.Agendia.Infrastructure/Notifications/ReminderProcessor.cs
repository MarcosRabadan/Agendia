using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Events;

namespace MRC.Agendia.Infrastructure.Notifications
{
    /// <summary>
    /// Publishes an <see cref="AppointmentReminder"/> event for each appointment starting
    /// within the reminder window that has not been reminded yet, marking ReminderSentAt in
    /// the same save (transactional outbox) so the event and the mark commit together.
    /// Extracted from <see cref="AppointmentReminderService"/> (which owns the timing loop)
    /// so it is unit-testable with an in-memory context.
    ///
    /// <para><b>N-instance safe.</b> On a real database it takes a session-level advisory
    /// lock (pg_try_advisory_lock) so only ONE instance runs the batch at a time; the others
    /// skip this cycle. Without it, N replicas would each select the same due appointments
    /// and publish the reminder N times (ReminderSentAt is only set after selection). The
    /// job is low-frequency, so mutual exclusion (not parallelism) is the right trade, and it
    /// preserves the per-item save that makes a single run crash-safe. The in-memory provider
    /// has no shared database to race against, so it just runs.</para>
    /// </summary>
    public class ReminderProcessor
    {
        // Two-int advisory-lock key. Postgres keeps a SEPARATE lock space for the two-int
        // form from the single-bigint form used by the booking guard (pg_advisory_xact_lock),
        // so this reminder lock can never collide with a booking lock.
        private const int LockClassId = 1;
        private const int LockObjectId = 42;

        private readonly AgendiaDbContext _context;
        private readonly IClock _clock;
        private readonly ReminderOptions _options;
        private readonly ILogger<ReminderProcessor> _logger;

        public ReminderProcessor(AgendiaDbContext context,
                                 IClock clock,
                                 IOptions<ReminderOptions> options,
                                 ILogger<ReminderProcessor> logger)
        {
            _context = context;
            _clock = clock;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Runs one reminder batch. Returns the number of reminders published (0 if another
        /// instance is already running the batch, or nothing is due).
        /// </summary>
        public async Task<int> ProcessDueAsync(CancellationToken cancellationToken = default)
        {
            // In-memory provider (unit tests): no shared DB to race against, run directly.
            if (!_context.Database.IsRelational())
                return await PublishDueRemindersAsync(cancellationToken);

            // Keep one connection open for the whole batch so the session-level advisory lock
            // is held across it, and release it in a finally.
            await _context.Database.OpenConnectionAsync(cancellationToken);
            try
            {
                if (!await TryAcquireLockAsync(cancellationToken))
                {
                    _logger.LogDebug("Reminder batch skipped: another instance is already running it.");
                    return 0;
                }

                try
                {
                    return await PublishDueRemindersAsync(cancellationToken);
                }
                finally
                {
                    await ReleaseLockAsync(cancellationToken);
                }
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        private async Task<int> PublishDueRemindersAsync(CancellationToken cancellationToken)
        {
            // Wall-clock "now" in the business timezone, to line up with the wall-clock
            // StartDate of appointments.
            var now = _clock.BusinessNow;
            var until = now + TimeSpan.FromHours(Math.Max(1, _options.ReminderWindowHours));

            // IgnoreQueryFilters + explicit conditions so a soft-deleted parent does not
            // silently drop rows via an INNER JOIN, while still excluding appointments whose
            // participants are gone or whose employee is inactive (those must not get reminders).
            var due = await _context.Appointments
                .IgnoreQueryFilters()
                .Include(a => a.Employee)
                    .ThenInclude(e => e.Business)
                .Where(a => !a.IsDeleted
                    && !a.Employee.IsDeleted
                    && a.Employee.IsActive
                    && !a.Employee.Business.IsDeleted
                    && a.ReminderSentAt == null
                    && a.StartDate > now
                    && a.StartDate <= until
                    && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed))
                .ToListAsync(cancellationToken);

            if (due.Count == 0)
                return 0;

            var published = 0;
            foreach (var appointment in due)
            {
                // Raise the reminder event on the appointment and mark ReminderSentAt: the
                // DbContext SaveChanges override enlists it into the outbox in the SAME save,
                // so the event and the mark commit together.
                appointment.RaiseEvent(new AppointmentReminder(
                    appointment.Id, appointment.Employee.BusinessId, appointment.EmployeeId,
                    appointment.ClientUserId, appointment.ServiceId, appointment.StartDate,
                    appointment.EndDate, appointment.Employee.Business.DefaultLanguage, _clock.TimeZoneId, DateTime.UtcNow));

                appointment.ReminderSentAt = DateTime.UtcNow;
                // Persist per item, NOT once after the loop: a crash mid-batch would otherwise
                // lose every ReminderSentAt mark and re-publish the already-emitted reminders.
                await _context.SaveChangesAsync(cancellationToken);
                published++;
            }

            return published;
        }

        private async Task<bool> TryAcquireLockAsync(CancellationToken cancellationToken)
        {
            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT pg_try_advisory_lock({LockClassId}, {LockObjectId})";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is true;
        }

        private async Task ReleaseLockAsync(CancellationToken cancellationToken)
        {
            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT pg_advisory_unlock({LockClassId}, {LockObjectId})";
            await command.ExecuteScalarAsync(cancellationToken);
        }
    }
}
