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
        DateTime CreatedAt);
}
