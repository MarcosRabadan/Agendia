namespace MRC.Agendia.Application.Schedules.DTO
{
    public record CustomTimeSlotDto(
        int Id,
        TimeOnly StartTime,
        TimeOnly EndTime);
}
