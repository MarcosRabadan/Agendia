namespace MRC.Agendia.Application.Schedules.DTO
{
    public record VacationPeriodDto(
        DateOnly From,
        DateOnly To,
        string? Reason);
}
