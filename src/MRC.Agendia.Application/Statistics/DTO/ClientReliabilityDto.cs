namespace MRC.Agendia.Application.Statistics.DTO
{
    /// <summary>
    /// Attendance record of one client (their Harmony "sub") inside a business over a
    /// recent window. Metrics only: Agendia does not own the client's profile, so this
    /// never carries a name or contact detail.
    ///
    /// <para>Only ELAPSED appointments are counted (start before "now"), because a future
    /// booking has no outcome yet. <see cref="NoShowRate"/> divides no-shows by the
    /// appointments that were actually meant to happen (completed + no-show), so
    /// cancellations - which freed the slot in advance - do not dilute it;
    /// <see cref="CancellationRate"/> is over the whole total. Appointments the staff
    /// never closed (still Pending/Confirmed after their time) count in
    /// <see cref="Total"/> but in neither rate.</para>
    /// </summary>
    public record ClientReliabilityDto(
        string ClientUserId,
        Guid BusinessId,
        DateOnly From,
        DateOnly To,
        int Total,
        int Completed,
        int NoShow,
        int Cancelled,
        double NoShowRate,
        double CancellationRate);
}
