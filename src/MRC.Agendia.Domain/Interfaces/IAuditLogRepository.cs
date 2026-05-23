using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IAuditLogRepository
    {
        Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedFilteredAsync(
            string? userId,
            string? action,
            string? entityType,
            DateTime? from,
            DateTime? to,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
