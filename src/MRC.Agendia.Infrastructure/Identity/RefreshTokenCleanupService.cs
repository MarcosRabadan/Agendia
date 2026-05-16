using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MRC.Agendia.Infrastructure.Identity
{
    /// <summary>
    /// Hosted service que elimina refresh tokens expirados de la base de datos.
    ///
    /// Sin esto, la tabla RefreshTokens crece indefinidamente porque cada login
    /// crea un token y cada refresh rota a otro nuevo, dejando el anterior
    /// revocado (pero almacenado).
    ///
    /// Configuracion (opcional, con defaults seguros):
    ///   "RefreshTokenCleanup": {
    ///     "IntervalHours": 24,
    ///     "RetentionDays": 30
    ///   }
    /// </summary>
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RefreshTokenCleanupService> _logger;
        private readonly TimeSpan _interval;
        private readonly TimeSpan _retention;

        public RefreshTokenCleanupService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<RefreshTokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            var section = configuration.GetSection("RefreshTokenCleanup");
            var intervalHours = section.GetValue<int?>("IntervalHours") ?? 24;
            var retentionDays = section.GetValue<int?>("RetentionDays") ?? 30;

            _interval = TimeSpan.FromHours(Math.Max(1, intervalHours));
            _retention = TimeSpan.FromDays(Math.Max(1, retentionDays));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "RefreshTokenCleanupService iniciado. Intervalo: {Interval}, Retencion: {Retention}",
                _interval, _retention);

            // Pequeno delay inicial para no bloquear el arranque
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
                    await CleanupExpiredTokensAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en la limpieza de refresh tokens. Se reintentara en {Interval}.", _interval);
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

        private async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

            var cutoff = DateTime.UtcNow - _retention;

            var deleted = await context.RefreshTokens
                .Where(rt => rt.ExpiresAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            if (deleted > 0)
            {
                _logger.LogInformation(
                    "Eliminados {Count} refresh tokens expirados (anteriores a {Cutoff:yyyy-MM-dd HH:mm:ss} UTC).",
                    deleted, cutoff);
            }
            else
            {
                _logger.LogDebug("No habia refresh tokens expirados para eliminar.");
            }
        }
    }
}
