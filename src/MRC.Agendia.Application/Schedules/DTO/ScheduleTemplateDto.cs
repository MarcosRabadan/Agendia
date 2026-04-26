namespace MRC.Agendia.Application.Schedules.DTO
{
    public record ScheduleTemplateDto(
        int Id,
        int BusinessId,
        string Name,
        DateOnly EffectiveFrom,
        DateOnly EffectiveTo,
        bool IsDefault,
        DateTime CreatedAt,
        List<WeeklyTimeSlotDto> WeeklySlots);
}
