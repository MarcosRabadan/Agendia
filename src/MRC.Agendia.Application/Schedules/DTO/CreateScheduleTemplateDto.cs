namespace MRC.Agendia.Application.Schedules.DTO
{
    public record CreateScheduleTemplateDto(
        Guid BusinessId,
        string Name,
        DateOnly EffectiveFrom,
        DateOnly EffectiveTo,
        bool IsDefault,
        List<CreateWeeklyTimeSlotDto> WeeklySlots);
}
