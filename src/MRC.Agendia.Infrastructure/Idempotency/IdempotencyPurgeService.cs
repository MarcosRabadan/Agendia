using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MRC.Agendia.Infrastructure.Idempotency
{
    /// <summary>
    /// Drops idempotency records once they age past the retention window, so the table
    /// stays bounded. Safe to run on several instances: the delete is idempotent by
    /// nature (each row disappears once, whoever gets there first).
    ///
    /// Configuration (optional, safe defaults): see <see cref="IdempotencyOptions"/>
    /// ("Idempotency" section: RetentionHours, PurgeIntervalMinutes).
    /// </summary>
    public class IdempotencyPurgeService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IdempotencyOptions _options;
        private readonly ILogger<IdempotencyPurgeService> _logger;

        public IdempotencyPurgeService(IServiceProvider serviceProvider,
                                       IOptions<IdempotencyOptions> options,
                                       ILogger<IdempotencyPurgeService> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMinutes(Math.Max(1, _options.PurgeIntervalMinutes));
            var retention = TimeSpan.FromHours(Math.Max(1, _options.RetentionHours));

            _logger.LogInformation(
                "IdempotencyPurgeService started. Interval: {Interval}, retention: {Retention}.",
                interval, retention);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

                    var threshold = DateTime.UtcNow - retention;
                    var purged = await context.IdempotencyRecords
                        .Where(r => r.CreatedAtUtc < threshold)
                        .ExecuteDeleteAsync(stoppingToken);

                    if (purged > 0)
                        _logger.LogInformation("Purged {Purged} expired idempotency record(s).", purged);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Idempotency purge cycle failed; retrying in {Interval}.", interval);
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
