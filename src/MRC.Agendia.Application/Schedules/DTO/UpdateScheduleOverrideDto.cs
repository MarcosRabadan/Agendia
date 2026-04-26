using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Schedules.DTO
{
    public record UpdateScheduleOverrideDto(
        int Id,
        DateOnly Date,
        ScheduleOverrideType OverrideType,
        string? Reason,
        List<CreateCustomTimeSlotDto>? CustomSlots);
}
