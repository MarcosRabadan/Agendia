using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Schedules.DTO
{
    public record CreateWeeklyTimeSlotDto(
        DayOfWeek DayOfWeek,
        TimeOnly StartTime,
        TimeOnly EndTime,
        TimeSlotType SlotType);
}
