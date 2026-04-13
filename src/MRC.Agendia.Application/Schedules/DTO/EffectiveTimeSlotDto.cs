namespace MRC.Agendia.Application.Schedules.DTO
{
    public record EffectiveTimeSlotDto(
        TimeOnly StartTime,
        TimeOnly EndTime);
}
