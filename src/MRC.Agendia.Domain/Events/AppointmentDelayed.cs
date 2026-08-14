namespace MRC.Agendia.Domain.Events
{
    /// <summary>
    /// Raised when staff report the business is running <see cref="DelayMinutes"/>
    /// minutes late, for each affected upcoming appointment. The consumer notifies
    /// the client (resolved from <see cref="ClientUserId"/>) in <see cref="Language"/>.
    /// </summary>
    public sealed record AppointmentDelayed(int AppointmentId,
                                            int BusinessId,
                                            int EmployeeId,
                                            string ClientUserId,
                                            int ServiceId,
                                            DateTime StartDate,
                                            DateTime EndDate,
                                            int DelayMinutes,
                                            string Language,
                                            DateTime OccurredOnUtc) : IIntegrationEvent;
}
