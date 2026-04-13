namespace MRC.Agendia.Application.Schedules.DTO
{
    public record CreateCustomTimeSlotDto(
        TimeOnly StartTime,
        TimeOnly EndTime);
}
