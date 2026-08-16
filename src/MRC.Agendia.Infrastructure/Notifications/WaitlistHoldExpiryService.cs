using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MRC.Agendia.Application.Waitlist;

namespace MRC.Agendia.Infrastructure.Notifications
{
    /// <summary>
    /// Timing loop for the waitlist hold expiry (#268). The work of a cycle lives in
    /// <see cref="WaitlistHoldProcessor"/>; this service only decides when to run it.
    ///
    /// Safe on several instances: each slot's expiry runs inside the booking concurrency
    /// guard, so two instances cannot hand the same slot to two clients.
    ///
    /// Configuration (optional, safe defaults): see <see cref="WaitlistOptions"/>
    /// ("Waitlist" section: HoldMinutes, ExpiryIntervalMinutes, ExpiryBatchSize).
    /// </summary>
    public class WaitlistHoldExpiryService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly WaitlistOptions _options;
        private readonly ILogger<WaitlistHoldExpiryService> _logger;

        public WaitlistHoldExpiryService(IServiceProvider serviceProvider,
                                         WaitlistOptions options,
                                         ILogger<WaitlistHoldExpiryService> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMinutes(Math.Max(1, _options.ExpiryIntervalMinutes));
            _logger.LogInformation(
                "WaitlistHoldExpiryService started. Interval: {Interval}, hold: {HoldMinutes} min.",
                interval, _options.HoldMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<WaitlistHoldProcessor>();
                    await processor.ExpireDueHoldsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Waitlist hold expiry cycle failed; retrying in {Interval}.", interval);
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
