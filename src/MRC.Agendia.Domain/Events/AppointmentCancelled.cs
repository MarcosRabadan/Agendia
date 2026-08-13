namespace MRC.Agendia.Domain.Events
{
    /// <summary>
    /// Raised when an appointment is cancelled. The consumer resolves the client's
    /// contact details from <see cref="ClientUserId"/> and delivers the
    /// cancellation notice in <see cref="Language"/>.
    /// </summary>
    public sealed record AppointmentCancelled(int AppointmentId,
                                              int BusinessId,
                                              int EmployeeId,
                                              string ClientUserId,
                                              int ServiceId,
                                              DateTime StartDate,
                                              DateTime EndDate,
                                              string Language,
                                              DateTime OccurredOnUtc) : IIntegrationEvent;
}
