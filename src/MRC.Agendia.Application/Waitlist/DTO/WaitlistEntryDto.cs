using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Waitlist.DTO
{
    public record WaitlistEntryDto(
        Guid Id,
        Guid BusinessId,
        Guid ServiceId,
        string ClientUserId,
        Guid? EmployeeId,
        DateOnly Date,
        TimeOnly StartTime,
        WaitlistStatus Status,
        DateTime CreatedAt,
        // While Notified, the UTC instant the client's priority hold on the slot runs out
        // (#268). Null once it is consumed, expired or never granted.
        DateTime? HoldUntil = null);
}
