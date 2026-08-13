namespace MRC.Agendia.Domain.Interfaces
{
    /// <summary>
    /// Projection with exactly the fields an appointment integration event needs,
    /// including the owning business's default language, so building an event is a
    /// single query rather than loading the full appointment graph.
    /// </summary>
    public sealed record AppointmentNotificationContext(int AppointmentId,
                                                        int BusinessId,
                                                        int EmployeeId,
                                                        string ClientUserId,
                                                        int ServiceId,
                                                        DateTime StartDate,
                                                        DateTime EndDate,
                                                        string Language);
}
