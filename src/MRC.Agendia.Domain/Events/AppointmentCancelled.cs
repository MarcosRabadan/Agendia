namespace MRC.Agendia.Domain.Events
{
    /// <summary>
    /// Raised when an appointment is cancelled. The consumer resolves the client's
    /// contact details from <see cref="ClientUserId"/> and delivers the
    /// cancellation notice in <see cref="Language"/>.
    /// </summary>
    public sealed record AppointmentCancelled(Guid AppointmentId,
                                              Guid BusinessId,
                                              Guid EmployeeId,
                                              string ClientUserId,
                                              Guid ServiceId,
                                              DateTime StartDate,
                                              DateTime EndDate,
                                              string Language,
                                              string TimeZone,
                                              DateTime OccurredOnUtc) : IIntegrationEvent;
}
