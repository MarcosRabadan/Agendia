using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly AgendiaDbContext _context;

        public AuditLogRepository(AgendiaDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedFilteredAsync(string? userId,
                                                                                           string? action,
                                                                                           string? entityType,
                                                                                           DateTime? from,
                                                                                           DateTime? to,
                                                                                           int page,
                                                                                           int pageSize,
                                                                                           CancellationToken cancellationToken = default)
        {
            var query = _context.AuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(a => a.UserId == userId);
            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(a => a.Action == action);
            if (!string.IsNullOrWhiteSpace(entityType))
                query = query.Where(a => a.EntityType == entityType);
            if (from.HasValue)
                query = query.Where(a => a.Timestamp >= from.Value);
            if (to.HasValue)
                query = query.Where(a => a.Timestamp <= to.Value);

            return query
                .OrderByDescending(a => a.Timestamp)
                .ToPagedListAsync(page, pageSize, cancellationToken);
        }
    }
}
