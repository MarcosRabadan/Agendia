using MRC.Agendia.Domain.Statistics;

namespace MRC.Agendia.Domain.Interfaces
{
    /// <summary>
    /// Read-only access to the data behind the business statistics panel.
    /// Returns a projected row per appointment in range (not the full entity).
    /// </summary>
    public interface IBusinessStatsRepository
    {
        Task<IReadOnlyList<AppointmentStatsRow>> GetAppointmentsAsync(
            int businessId,
            DateTime fromInclusive,
            DateTime toExclusive,
            CancellationToken cancellationToken = default);
    }
}
