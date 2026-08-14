namespace MRC.Agendia.Application.Schedules.DTO
{
    public record GenerateScheduleRequestDto(
        Guid BusinessId,
        int Year,
        List<GenerateScheduleTemplateInputDto> Templates,
        bool IncludeNationalHolidays,
        bool IncludeLocalHolidays,
        List<VacationPeriodDto>? VacationPeriods,
        List<ClosedDateDto>? CustomClosedDates,
        bool ReplaceExisting = false);
}
