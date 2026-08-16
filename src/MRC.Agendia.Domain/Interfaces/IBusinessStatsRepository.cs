using MRC.Agendia.Domain.Enums;
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
        Task<IReadOnlyList<AppointmentStatsRow>> GetAppointmentsAsync(Guid businessId,
                                                                      DateTime fromInclusive,
                                                                      DateTime toExclusive,
                                                                      CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the status of every live appointment a client (their Harmony user id) has
        /// in a business whose start falls in [fromInclusive, toExclusive). Only the status
        /// is projected: it is all the reliability metrics need. Same soft-delete semantics
        /// as <see cref="GetAppointmentsAsync"/>.
        /// </summary>
        /// <param name="businessId">Business id (resolved through the employee).</param>
        /// <param name="clientUserId">The client's Harmony user id ("sub").</param>
        /// <param name="fromInclusive">Range start (inclusive).</param>
        /// <param name="toExclusive">Range end (exclusive).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The status of each appointment in the range.</returns>
        Task<IReadOnlyList<AppointmentStatus>> GetClientAppointmentStatusesAsync(Guid businessId,
                                                                                 string clientUserId,
                                                                                 DateTime fromInclusive,
                                                                                 DateTime toExclusive,
                                                                                 CancellationToken cancellationToken = default);
    }
}
