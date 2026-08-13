using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Events;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Events;

namespace MRC.Agendia.Infrastructure.Notifications
{
    /// <summary>
    /// Hosted service that publishes an <see cref="AppointmentReminder"/> event for
    /// appointments starting within the next <c>ReminderWindowHours</c> (24h by
    /// default) that have not been reminded yet. Idempotent via
    /// <c>Appointment.ReminderSentAt</c>. Delivery is done by the consumer; Agendia
    /// only raises the event.
    ///
    /// Configuration (optional, with safe defaults):
    ///   "Notifications": {
    ///     "ReminderIntervalMinutes": 60,
    ///     "ReminderWindowHours": 24
    ///   }
    /// </summary>
    public class AppointmentReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IClock _clock;
        private readonly ILogger<AppointmentReminderService> _logger;
        private readonly TimeSpan _interval;
        private readonly TimeSpan _window;

        public AppointmentReminderService(IServiceProvider serviceProvider,
                                          IClock clock,
                                          IConfiguration configuration,
                                          ILogger<AppointmentReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _clock = clock;
            _logger = logger;

            var section = configuration.GetSection("Notifications");
            var intervalMinutes = section.GetValue<int?>("ReminderIntervalMinutes") ?? 60;
            var windowHours = section.GetValue<int?>("ReminderWindowHours") ?? 24;

            _interval = TimeSpan.FromMinutes(Math.Max(1, intervalMinutes));
            _window = TimeSpan.FromHours(Math.Max(1, windowHours));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "AppointmentReminderService iniciado. Intervalo: {Interval}, Ventana: {Window}.",
                _interval, _window);

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendDueRemindersAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando recordatorios. Se reintentara en {Interval}.", _interval);
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task SendDueRemindersAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            // Same scope as the context, so enlisting an event and marking
            // ReminderSentAt are persisted by the same SaveChanges (outbox).
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            // Wall-clock "now" in the business timezone, to line up with the
            // wall-clock StartDate of appointments.
            var now = _clock.BusinessNow;
            var until = now + _window;

            // IgnoreQueryFilters + explicit conditions so a soft-deleted parent
            // (client/employee/business) does not silently drop rows via an INNER
            // JOIN, while still excluding appointments whose participants are gone
            // or whose employee is inactive (those must not get reminders).
            var due = await context.Appointments
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
            {
                _logger.LogDebug("No hay citas pendientes de recordatorio.");
                return;
            }

            var published = 0;
            foreach (var appointment in due)
            {
                // Enlist the reminder event and mark ReminderSentAt in the SAME save
                // (transactional outbox): the event and the mark commit together.
                await eventPublisher.PublishAsync(new AppointmentReminder(
                    appointment.Id, appointment.Employee.BusinessId, appointment.EmployeeId,
                    appointment.ClientUserId, appointment.ServiceId, appointment.StartDate,
                    appointment.EndDate, appointment.Employee.Business.DefaultLanguage, DateTime.UtcNow),
                    cancellationToken);

                appointment.ReminderSentAt = DateTime.UtcNow;
                // Persist per item, NOT once after the whole loop: otherwise a
                // crash/recycle mid-batch loses every ReminderSentAt mark and
                // re-publishes all the already-emitted reminders on the next run.
                // (This makes a single-instance run crash-safe. Running multiple
                // instances concurrently would additionally need a RowVersion /
                // atomic claim to avoid double-emits; single-instance today.)
                await context.SaveChangesAsync(cancellationToken);
                published++;
            }

            _logger.LogInformation("Publicados {Published} de {Total} recordatorio(s) de cita.", published, due.Count);
        }
    }
}
