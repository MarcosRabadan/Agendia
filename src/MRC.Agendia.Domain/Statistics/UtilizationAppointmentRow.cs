namespace MRC.Agendia.Domain.Statistics
{
    /// <summary>
    /// Read-model row for the agenda utilization report: only what the aggregation needs
    /// from an appointment that occupied the agenda.
    ///
    /// <para><see cref="StartDate"/>/<see cref="EndDate"/> are wall clock;
    /// <see cref="CreatedAtUtc"/> is a real instant. The lead time is the gap between the
    /// two, so the caller converts the instant to the business wall clock before handing
    /// the row to the calculator.</para>
    /// </summary>
    public record UtilizationAppointmentRow(
        Guid EmployeeId,
        DateTime StartDate,
        DateTime EndDate,
        DateTime CreatedAtUtc);
}
