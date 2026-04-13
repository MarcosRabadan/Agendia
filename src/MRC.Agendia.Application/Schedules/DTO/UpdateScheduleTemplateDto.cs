namespace MRC.Agendia.Application.Schedules.DTO
{
    public record UpdateScheduleTemplateDto(
        int Id,
        string Name,
        DateOnly EffectiveFrom,
        DateOnly EffectiveTo,
        bool IsDefault,
        List<CreateWeeklyTimeSlotDto> WeeklySlots);
}
