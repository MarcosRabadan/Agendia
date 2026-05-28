using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Domain.Statistics
{
    /// <summary>
    /// Lightweight read-model row for business statistics: only the columns the
    /// aggregation needs, projected server-side so the whole appointment graph is
    /// never loaded.
    /// </summary>
    public record AppointmentStatsRow(
        DateTime StartDate,
        AppointmentStatus Status,
        int ServiceId,
        string ServiceName,
        decimal ServicePrice);
}
