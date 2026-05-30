using MRC.Agendia.Domain.Statistics;

namespace MRC.Agendia.Domain.Interfaces
{
    /// <summary>
    /// Read-only access to the data behind the business statistics panel.
    /// Returns a projected row per appointment in range (not the full entity).
    /// </summary>
    public interface IBusinessStatsRepository
    {
        /// <summary>
        /// Gets one projected stats row per live appointment of a business whose start
        /// falls in [fromInclusive, toExclusive). Untracked; ignores the soft-delete
        /// filter so a historical appointment whose service was later soft-deleted is
        /// still counted (only soft-deleted appointments themselves are excluded).
        /// </summary>
        /// <param name="businessId">Business id (resolved through the employee).</param>
        /// <param name="fromInclusive">Range start (inclusive).</param>
        /// <param name="toExclusive">Range end (exclusive).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The projected appointment rows for the range.</returns>
        Task<IReadOnlyList<AppointmentStatsRow>> GetAppointmentsAsync(int businessId,
                                                                      DateTime fromInclusive,
                                                                      DateTime toExclusive,
                                                                      CancellationToken cancellationToken = default);
    }
}
