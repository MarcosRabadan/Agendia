namespace MRC.Agendia.Application.BusinessSchedule.DTO
{
    public record CreateBusinessScheduleDto(int BusinessId, int DayOfWeek, TimeSpan StartTime, TimeSpan EndTime, bool IsWorkingDay);
}
