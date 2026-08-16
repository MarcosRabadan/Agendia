namespace MRC.Agendia.Domain.Events
{
    /// <summary>
    /// Raised when an appointment's time is moved (rescheduled) to a different slot. Carries
    /// the previous and the new start/end so the consumer can tell the client "your appointment
    /// moved from X to Y". The consumer resolves the client's contact details from
    /// <see cref="ClientUserId"/> and delivers in <see cref="Language"/>.
    /// </summary>
    public sealed record AppointmentRescheduled(Guid AppointmentId,
                                                Guid BusinessId,
                                                Guid EmployeeId,
                                                string ClientUserId,
                                                Guid ServiceId,
                                                DateTime PreviousStartDate,
                                                DateTime PreviousEndDate,
                                                DateTime StartDate,
                                                DateTime EndDate,
                                                string Language,
                                                DateTime OccurredOnUtc) : IIntegrationEvent;
}
