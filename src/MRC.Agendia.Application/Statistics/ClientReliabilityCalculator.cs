using MRC.Agendia.Application.Statistics.DTO;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Statistics
{
    /// <summary>
    /// Pure, in-memory aggregation of a client's appointment outcomes into the
    /// reliability metrics. Kept apart from the data access so the rates can be
    /// unit-tested against a known set of statuses (same split as
    /// <see cref="BusinessStatsCalculator"/>).
    /// </summary>
    public static class ClientReliabilityCalculator
    {
        public static ClientReliabilityDto Calculate(IReadOnlyList<AppointmentStatus> statuses,
                                                     Guid businessId,
                                                     string clientUserId,
                                                     DateOnly from,
                                                     DateOnly to)
        {
            var completed = statuses.Count(s => s == AppointmentStatus.Completed);
            var noShow = statuses.Count(s => s == AppointmentStatus.NoShow);
            var cancelled = statuses.Count(s => s == AppointmentStatus.Cancelled);

            // Denominator: the appointments that were meant to happen. A cancellation
            // freed the slot beforehand, so it is not a missed attendance; it gets its
            // own rate over the total instead.
            var attendanceOpportunities = completed + noShow;

            return new ClientReliabilityDto(
                clientUserId,
                businessId,
                from,
                to,
                statuses.Count,
                completed,
                noShow,
                cancelled,
                Rate(noShow, attendanceOpportunities),
                Rate(cancelled, statuses.Count));
        }

        // Rounded so the payload does not leak binary-fraction noise (0.30000000000000004).
        private static double Rate(int count, int total) =>
            total == 0 ? 0 : Math.Round((double)count / total, 4);
    }
}
