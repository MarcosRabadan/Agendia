namespace MRC.Agendia.Application.Schedules.DTO
{
    public record ClosedDateDto(
        DateOnly Date,
        string? Reason);
}
