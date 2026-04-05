namespace MRC.Agendia.Application.BusinessSchedule.DTO
{
    public record BusinessScheduleDto(int Id, int BusinessId, int DayOfWeek, TimeSpan StartTime, TimeSpan EndTime, bool IsWorkingDay);
    public record CreateBusinessScheduleDto(int BusinessId, int DayOfWeek, TimeSpan StartTime, TimeSpan EndTime, bool IsWorkingDay);
    public record UpdateBusinessScheduleDto(int Id, int BusinessId, int DayOfWeek, TimeSpan StartTime, TimeSpan EndTime, bool IsWorkingDay);
}
