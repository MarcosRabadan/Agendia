namespace MRC.Agendia.Application.Schedules.DTO
{
    public record CalendarDayDto(
        DateOnly Date,
        bool IsOpen,
        string? Status,
        List<EffectiveTimeSlotDto>? TimeSlots);
}
