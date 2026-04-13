namespace MRC.Agendia.Application.Schedules.DTO
{
    public record GenerateScheduleRequestDto(
        int BusinessId,
        string Name,
        DateOnly EffectiveFrom,
        DateOnly EffectiveTo,
        List<CreateWeeklyTimeSlotDto> WeeklySlots,
        bool IncludeNationalHolidays,
        bool IncludeLocalHolidays,
        string? Region,
        List<VacationPeriodDto>? VacationPeriods,
        List<ClosedDateDto>? CustomClosedDates);
}
