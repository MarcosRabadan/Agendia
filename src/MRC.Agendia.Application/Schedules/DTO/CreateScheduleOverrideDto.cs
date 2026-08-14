using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Schedules.DTO
{
    public record CreateScheduleOverrideDto(
        Guid BusinessId,
        DateOnly Date,
        ScheduleOverrideType OverrideType,
        string? Reason,
        List<CreateCustomTimeSlotDto>? CustomSlots);
}
