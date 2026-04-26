namespace MRC.Agendia.Application.Schedules.DTO
{
    public record GenerateScheduleRequestDto(
        int BusinessId,
        int Year,
        List<GenerateScheduleTemplateInputDto> Templates,
        bool IncludeNationalHolidays,
        bool IncludeLocalHolidays,
        List<VacationPeriodDto>? VacationPeriods,
        List<ClosedDateDto>? CustomClosedDates);
}
