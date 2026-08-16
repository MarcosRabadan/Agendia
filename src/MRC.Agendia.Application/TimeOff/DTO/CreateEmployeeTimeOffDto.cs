namespace MRC.Agendia.Application.TimeOff.DTO
{
    /// <summary>Request to block an employee's agenda for a wall-clock range.</summary>
    public record CreateEmployeeTimeOffDto(
        DateTime Start,
        DateTime End,
        string? Reason = null);
}
