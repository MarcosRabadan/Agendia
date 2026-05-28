namespace MRC.Agendia.Application.Appointments.DTO
{
    /// <summary>
    /// Request to warn upcoming clients that the business is running late.
    /// <see cref="EmployeeId"/> null = the whole business; otherwise only that
    /// employee's queue. <see cref="MaxAppointments"/> caps how many of the next
    /// appointments are notified.
    /// </summary>
    public record NotifyDelayDto(
        int? EmployeeId,
        int DelayMinutes,
        int? MaxAppointments);
}
