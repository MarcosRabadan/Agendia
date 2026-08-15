namespace MRC.Agendia.Application.Schedules.DTO
{
    public record UpdateScheduleTemplateDto(
        Guid Id,
        string Name,
        DateOnly EffectiveFrom,
        DateOnly EffectiveTo,
        bool IsDefault,
        List<CreateWeeklyTimeSlotDto> WeeklySlots);
}
