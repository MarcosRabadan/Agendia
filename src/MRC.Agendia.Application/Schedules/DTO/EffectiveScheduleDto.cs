using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Schedules.DTO
{
    public record EffectiveScheduleDto(
        DateOnly Date,
        bool IsOpen,
        string? ClosedReason,
        ScheduleOverrideType? OverrideType,
        List<EffectiveTimeSlotDto> TimeSlots);
}
