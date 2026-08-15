using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Schedules.DTO
{
    public record ScheduleOverrideDto(
        Guid Id,
        Guid BusinessId,
        DateOnly Date,
        ScheduleOverrideType OverrideType,
        string? Reason,
        DateTime CreatedAt,
        List<CustomTimeSlotDto>? CustomSlots);
}
