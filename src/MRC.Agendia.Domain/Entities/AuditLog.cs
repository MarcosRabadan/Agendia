namespace MRC.Agendia.Domain.Entities
{
    /// <summary>
    /// Append-only record of a sensitive action (who did what, when, from where)
    /// for debugging, support and compliance.
    /// </summary>
    public class AuditLog
    {
        public long Id { get; set; }
        public string Action { get; set; } = null!;
        public string? UserId { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }
    }
}
