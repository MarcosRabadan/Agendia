namespace MRC.Agendia.Application.TimeOff.DTO
{
    /// <summary>A block on an employee's agenda. Wall-clock, half-open [Start, End).</summary>
    public record EmployeeTimeOffDto(
        Guid Id,
        Guid EmployeeId,
        DateTime Start,
        DateTime End,
        string? Reason);
}
