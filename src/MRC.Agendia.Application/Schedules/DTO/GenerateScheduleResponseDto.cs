namespace MRC.Agendia.Application.Schedules.DTO
{
    public record GenerateScheduleResponseDto(
        List<int> TemplateIds,
        int TotalWorkingDays,
        int TotalHolidays,
        int TotalVacationDays,
        int TotalClosedDays,
        List<string>? Warnings);
}
