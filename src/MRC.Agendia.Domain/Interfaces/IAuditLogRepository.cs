using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IAuditLogRepository
    {
        /// <summary>
        /// Gets a page of audit log entries matching the optional filters, newest first.
        /// Each filter is applied only when its argument is provided.
        /// </summary>
        /// <param name="userId">Filter by acting user id, or null for any.</param>
        /// <param name="action">Filter by action code, or null for any.</param>
        /// <param name="entityType">Filter by affected entity type, or null for any.</param>
        /// <param name="from">Only entries at or after this timestamp, or null for no lower bound.</param>
        /// <param name="to">Only entries at or before this timestamp, or null for no upper bound.</param>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The matching page of audit logs and the total count.</returns>
        Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedFilteredAsync(string? userId,
                                                                                    string? action,
                                                                                    string? entityType,
                                                                                    DateTime? from,
                                                                                    DateTime? to,
                                                                                    int page,
                                                                                    int pageSize,
                                                                                    CancellationToken cancellationToken = default);
    }
}
