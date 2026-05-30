namespace MRC.Agendia.Application.Auditing
{
    /// <summary>
    /// Records sensitive actions to the audit trail. The implementation fills in
    /// the current user and client IP from the request context. Best-effort: a
    /// logging failure is swallowed and never breaks the audited operation.
    /// </summary>
    public interface IAuditLogger
    {
        /// <summary>Writes one audit entry for the given action.</summary>
        /// <param name="action">Action code being audited (see AuditActions).</param>
        /// <param name="entityType">Optional type of the affected entity.</param>
        /// <param name="entityId">Optional id of the affected entity.</param>
        /// <param name="details">Optional extra data, serialized to JSON.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task LogAsync(string action,
                      string? entityType = null,
                      string? entityId = null,
                      object? details = null,
                      CancellationToken cancellationToken = default);
    }
}
