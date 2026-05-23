namespace MRC.Agendia.Application.Auditing
{
    /// <summary>
    /// Records sensitive actions to the audit trail. The implementation fills in
    /// the current user and client IP from the request context. Best-effort: a
    /// logging failure is swallowed and never breaks the audited operation.
    /// </summary>
    public interface IAuditLogger
    {
        Task LogAsync(
            string action,
            string? entityType = null,
            string? entityId = null,
            object? details = null,
            CancellationToken cancellationToken = default);
    }
}
