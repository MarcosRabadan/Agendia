using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MRC.Agendia.Application.Notifications;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Infrastructure.Notifications
{
    /// <summary>
    /// Hosted service that sends a reminder email for appointments starting
    /// within the next <c>ReminderWindowHours</c> (24h by default) that have not
    /// been reminded yet. Idempotent via <c>Appointment.ReminderSentAt</c>.
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
        private readonly ILogger<AppointmentReminderService> _logger;
        private readonly TimeSpan _interval;
        private readonly TimeSpan _window;

        public AppointmentReminderService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<AppointmentReminderService> logger)
        {
            _serviceProvider = serviceProvider;
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
            var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var now = DateTime.UtcNow;
            var until = now + _window;

            var due = await context.Appointments
                .Where(a => a.ReminderSentAt == null
                    && a.StartDate > now
                    && a.StartDate <= until
                    && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed))
                .ToListAsync(cancellationToken);

            if (due.Count == 0)
            {
                _logger.LogDebug("No hay citas pendientes de recordatorio.");
                return;
            }

            var sent = 0;
            foreach (var appointment in due)
            {
                // Only mark as reminded when the send actually succeeded, so a
                // transient failure is retried on the next run instead of being
                // silently lost.
                if (await notifications.SendAppointmentReminderAsync(appointment.Id, cancellationToken))
                {
                    appointment.ReminderSentAt = now;
                    sent++;
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Enviados {Sent} de {Total} recordatorio(s) de cita.", sent, due.Count);
        }
    }
}
