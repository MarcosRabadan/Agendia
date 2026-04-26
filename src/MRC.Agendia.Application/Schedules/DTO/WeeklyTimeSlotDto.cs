using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Schedules.DTO
{
    public record WeeklyTimeSlotDto(
        int Id,
        DayOfWeek DayOfWeek,
        TimeOnly StartTime,
        TimeOnly EndTime,
        TimeSlotType SlotType);
}
