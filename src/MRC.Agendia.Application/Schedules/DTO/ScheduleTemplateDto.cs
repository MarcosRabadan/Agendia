namespace MRC.Agendia.Application.Schedules.DTO
{
    public record ScheduleTemplateDto(
        Guid Id,
        Guid BusinessId,
        string Name,
        DateOnly EffectiveFrom,
        DateOnly EffectiveTo,
        bool IsDefault,
        DateTime CreatedAt,
        List<WeeklyTimeSlotDto> WeeklySlots);
}
