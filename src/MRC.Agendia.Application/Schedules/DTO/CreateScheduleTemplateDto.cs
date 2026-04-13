namespace MRC.Agendia.Application.Schedules.DTO
{
    public record CreateScheduleTemplateDto(
        int BusinessId,
        string Name,
        DateOnly EffectiveFrom,
        DateOnly EffectiveTo,
        bool IsDefault,
        List<CreateWeeklyTimeSlotDto> WeeklySlots);
}
