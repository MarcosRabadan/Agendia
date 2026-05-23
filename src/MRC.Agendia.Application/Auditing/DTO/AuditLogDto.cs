namespace MRC.Agendia.Application.Auditing.DTO
{
    public record AuditLogDto(
        long Id,
        string Action,
        string? UserId,
        string? EntityType,
        string? EntityId,
        string? Details,
        DateTime Timestamp,
        string? IpAddress);
}
