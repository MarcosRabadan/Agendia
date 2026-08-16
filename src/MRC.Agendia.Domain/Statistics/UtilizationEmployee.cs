namespace MRC.Agendia.Domain.Statistics
{
    /// <summary>
    /// An employee as the utilization report sees them: an id and how many appointments
    /// they can hold at once, which is what turns open minutes into offered capacity.
    /// </summary>
    public record UtilizationEmployee(Guid Id, int MaxConcurrentAppointments);
}
