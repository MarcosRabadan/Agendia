namespace MRC.Agendia.Application.Schedules.DTO
{
    public record GenerateScheduleResponseDto(
        List<Guid> TemplateIds,
        int TotalWorkingDays,
        int TotalHolidays,
        int TotalVacationDays,
        int TotalClosedDays,
        List<string>? Warnings);
}
