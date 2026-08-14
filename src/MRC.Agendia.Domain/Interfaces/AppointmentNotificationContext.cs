namespace MRC.Agendia.Domain.Interfaces
{
    /// <summary>
    /// Projection with exactly the fields an appointment integration event needs,
    /// including the owning business's default language, so building an event is a
    /// single query rather than loading the full appointment graph.
    /// </summary>
    public sealed record AppointmentNotificationContext(Guid AppointmentId,
                                                        Guid BusinessId,
                                                        Guid EmployeeId,
                                                        string ClientUserId,
                                                        Guid ServiceId,
                                                        DateTime StartDate,
                                                        DateTime EndDate,
                                                        string Language);
}
