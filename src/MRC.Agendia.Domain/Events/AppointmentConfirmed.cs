namespace MRC.Agendia.Domain.Events
{
    /// <summary>
    /// Raised when an appointment is booked. The consumer resolves the client's
    /// contact details from <see cref="ClientUserId"/> and delivers the
    /// confirmation in <see cref="Language"/>. Agendia carries no email/name.
    /// </summary>
    public sealed record AppointmentConfirmed(Guid AppointmentId,
                                              Guid BusinessId,
                                              Guid EmployeeId,
                                              string ClientUserId,
                                              Guid ServiceId,
                                              DateTime StartDate,
                                              DateTime EndDate,
                                              string Language,
                                              DateTime OccurredOnUtc) : IIntegrationEvent;
}
