namespace MRC.Agendia.Application.Schedules.DTO
{
    public record CustomTimeSlotDto(
        Guid Id,
        TimeOnly StartTime,
        TimeOnly EndTime);
}
