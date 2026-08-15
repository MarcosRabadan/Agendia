namespace MRC.Agendia.Application.Waitlist.DTO
{
    /// <summary>
    /// Request to join the waitlist of a full slot. <see cref="EmployeeId"/> null
    /// means "any employee that offers the service".
    /// </summary>
    public record JoinWaitlistDto(
        Guid BusinessId,
        Guid ServiceId,
        DateOnly Date,
        TimeOnly StartTime,
        Guid? EmployeeId);
}
