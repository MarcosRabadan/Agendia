namespace MRC.Agendia.Application.Schedules.DTO
{
    public record GenerateScheduleResponseDto(
        int TemplateId,
        int TotalWorkingDays,
        int TotalHolidays,
        int TotalVacationDays,
        int TotalClosedDays,
        List<string>? Warnings);
}
