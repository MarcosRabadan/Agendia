using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Schedules.DTO
{
    public record ScheduleOverrideDto(
        int Id,
        int BusinessId,
        DateOnly Date,
        ScheduleOverrideType OverrideType,
        string? Reason,
        DateTime CreatedAt,
        List<CustomTimeSlotDto>? CustomSlots);
}
