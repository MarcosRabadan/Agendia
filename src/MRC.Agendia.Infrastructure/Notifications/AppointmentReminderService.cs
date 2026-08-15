using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MRC.Agendia.Infrastructure.Notifications
{
    /// <summary>
    /// Hosted service that periodically runs the appointment reminder batch via
    /// <see cref="ReminderProcessor"/>. This service owns only the timing loop; the actual
    /// work - selecting due appointments, publishing the <c>AppointmentReminder</c> event,
    /// and the N-instance advisory lock - lives in the processor.
    ///
    /// Configuration (optional, safe defaults): see <see cref="ReminderOptions"/> ("Notifications"
    /// section: ReminderIntervalMinutes, ReminderWindowHours).
    /// </summary>
    public class AppointmentReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ReminderOptions _options;
        private readonly ILogger<AppointmentReminderService> _logger;

        public AppointmentReminderService(IServiceProvider serviceProvider,
                                          IOptions<ReminderOptions> options,
                                          ILogger<AppointmentReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMinutes(Math.Max(1, _options.ReminderIntervalMinutes));
            _logger.LogInformation(
                "AppointmentReminderService started (single active instance via advisory lock). Interval: {Interval}, window: {WindowHours}h.",
                interval, _options.ReminderWindowHours);

            // Small startup delay so the app is fully up before the first batch.
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
                    using var scope = _serviceProvider.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<ReminderProcessor>();

                    var published = await processor.ProcessDueAsync(stoppingToken);
                    if (published > 0)
                        _logger.LogInformation("Published {Published} appointment reminder(s).", published);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Reminder batch failed; retrying in {Interval}.", interval);
                }

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
