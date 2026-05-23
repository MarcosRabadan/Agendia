using System.Text.Json;
using Microsoft.Extensions.Logging;
using MRC.Agendia.Application.Auditing;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Infrastructure.Auditing
{
    /// <summary>
    /// Writes audit entries to the database, filling in the current user and
    /// client IP from <see cref="ICurrentUserContext"/>. Best-effort: a failure
    /// is logged and swallowed so it never breaks the audited operation. Callers
    /// must invoke this AFTER persisting their own changes (it calls SaveChanges
    /// on the shared context, so any pending changes would be flushed too).
    /// </summary>
    public class AuditLogger : IAuditLogger
    {
        private readonly AgendiaDbContext _context;
        private readonly ICurrentUserContext _currentUser;
        private readonly ILogger<AuditLogger> _logger;

        public AuditLogger(
            AgendiaDbContext context,
            ICurrentUserContext currentUser,
            ILogger<AuditLogger> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task LogAsync(
            string action,
            string? entityType = null,
            string? entityId = null,
            object? details = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var entry = new AuditLog
                {
                    Action = action,
                    UserId = _currentUser.UserId,
                    EntityType = entityType,
                    EntityId = entityId,
                    Details = details is null ? null : JsonSerializer.Serialize(details),
                    Timestamp = DateTime.UtcNow,
                    IpAddress = _currentUser.IpAddress
                };

                await _context.AuditLogs.AddAsync(entry, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log for action {Action}.", action);
            }
        }
    }
}
