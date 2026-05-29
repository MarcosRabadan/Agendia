using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Waitlist.DTO
{
    public record WaitlistEntryDto(
        int Id,
        int BusinessId,
        int ServiceId,
        int ClientId,
        int? EmployeeId,
        DateOnly Date,
        TimeOnly StartTime,
        WaitlistStatus Status,
        DateTime CreatedAt);
}
