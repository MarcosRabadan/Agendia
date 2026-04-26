namespace MRC.Agendia.Application.Schedules.DTO
{
    public record GenerateScheduleTemplateInputDto(
        string Name,
        DateOnly EffectiveFrom,
        DateOnly EffectiveTo,
        bool IsDefault,
        List<CreateWeeklyTimeSlotDto> WeeklySlots);
}
