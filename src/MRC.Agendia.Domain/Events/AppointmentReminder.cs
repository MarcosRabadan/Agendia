namespace MRC.Agendia.Domain.Events
{
    /// <summary>
    /// Raised by the reminder job for an appointment starting within the reminder
    /// window (24h). Emitted once per appointment (idempotency is kept via
    /// <c>Appointment.ReminderSentAt</c>). The consumer delivers the reminder in
    /// <see cref="Language"/> to the client resolved from <see cref="ClientUserId"/>.
    /// </summary>
    public sealed record AppointmentReminder(int AppointmentId,
                                             int BusinessId,
                                             int EmployeeId,
                                             string ClientUserId,
                                             int ServiceId,
                                             DateTime StartDate,
                                             DateTime EndDate,
                                             string Language,
                                             DateTime OccurredOnUtc) : IIntegrationEvent;
}
