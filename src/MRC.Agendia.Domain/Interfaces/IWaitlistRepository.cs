using MRC.Agendia.Domain.Availability;
using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IWaitlistRepository
    {
        /// <summary>Gets a tracked waitlist entry by id.</summary>
        /// <param name="id">Waitlist entry id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The entry, or null when missing.</returns>
        Task<WaitlistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Adds a new waitlist entry to the context.</summary>
        /// <param name="entry">The entry to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task AddAsync(WaitlistEntry entry, CancellationToken cancellationToken = default);

        /// <summary>Marks a waitlist entry as modified.</summary>
        /// <param name="entry">The entry to update.</param>
        void Update(WaitlistEntry entry);

        /// <summary>True if the client already has a Waiting entry for this exact slot.</summary>
        /// <param name="clientUserId">Harmony user id of the client.</param>
        /// <param name="businessId">Business id.</param>
        /// <param name="serviceId">Service id.</param>
        /// <param name="date">Slot date.</param>
        /// <param name="startTime">Slot start time.</param>
        /// <param name="employeeId">Requested employee id, or null for "any employee".</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True when a matching Waiting entry already exists.</returns>
        Task<bool> ExistsWaitingAsync(
            string clientUserId, Guid businessId, Guid serviceId, DateOnly date, TimeOnly startTime, Guid? employeeId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The Waiting entries that a freed slot could serve, FIFO by CreatedAt: same
        /// business/service/date, either "any employee" or that employee, and a slot that
        /// OVERLAPS the freed window. Tracked so the caller can mark one Notified.
        ///
        /// <para>Overlap, not an exact start time (#350): joining the queue is allowed when the
        /// slot is full, and fullness is measured by overlap, so somebody can wait at 10:30 for
        /// a class that runs 10:00-11:00. Matching the notification by exact start left those
        /// entries in a queue they would never be called from.</para>
        ///
        /// <para>The window is expressed as two bounds on StartTime rather than a join with
        /// Service: every candidate shares <paramref name="serviceId"/>, so they all have the
        /// same duration and the overlap collapses to constants the caller computes once. Either
        /// bound may be null, meaning "unbounded on that side" - which is what a freed window
        /// reaching past midnight, or starting less than one duration after it, really means.</para>
        /// </summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="serviceId">Service id.</param>
        /// <param name="date">Freed slot date.</param>
        /// <param name="windowEnd">Exclusive upper bound for StartTime (the end of the freed window), or null for none.</param>
        /// <param name="earliestStart">Exclusive lower bound for StartTime (freed start minus the service duration), or null for none.</param>
        /// <param name="employeeId">Id of the employee whose slot was freed.</param>
        /// <param name="maxCandidates">Hard cap on rows returned: the caller re-checks capacity per candidate inside the booking lock.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The matching Waiting entries (excluding soft-deleted participants), oldest first.</returns>
        Task<IReadOnlyList<WaitlistEntry>> GetWaitingCandidatesForSlotAsync(
            Guid businessId,
            Guid serviceId,
            DateOnly date,
            TimeOnly? windowEnd,
            TimeOnly? earliestStart,
            Guid employeeId,
            int maxCandidates,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The active priority holds (#268) of a business on a date: entries Notified whose
        /// HoldUntil has not passed. Untracked - the callers only count these seats.
        /// Returned already resolved to the window each hold covers, so nobody has to
        /// recompute it and drift from the others (#308).
        /// </summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="date">Slot date (wall clock).</param>
        /// <param name="nowUtc">Current UTC instant, to drop the expired ones.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The seats still held that day, one per active hold.</returns>
        Task<IReadOnlyList<SlotHold>> GetActiveHoldsAsync(Guid businessId,
                                                          DateOnly date,
                                                          DateTime nowUtc,
                                                          CancellationToken cancellationToken = default);

        /// <summary>
        /// The holds whose window has already run out (Notified with HoldUntil in the past),
        /// oldest first, so the expiry job can move the queue on. Tracked.
        /// </summary>
        /// <param name="nowUtc">Current UTC instant.</param>
        /// <param name="batchSize">Maximum entries to return.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The expired holds.</returns>
        Task<IReadOnlyList<WaitlistEntry>> GetExpiredHoldsAsync(DateTime nowUtc,
                                                                int batchSize,
                                                                CancellationToken cancellationToken = default);

        /// <summary>
        /// The client's active hold on a slot, if any: what a booking on that exact slot
        /// consumes (#268). Tracked so the caller can mark it Booked.
        /// </summary>
        /// <param name="clientUserId">Harmony user id of the client.</param>
        /// <param name="date">Slot date.</param>
        /// <param name="startTime">Slot start time.</param>
        /// <param name="employeeId">Employee the appointment is booked with.</param>
        /// <param name="nowUtc">Current UTC instant.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The matching hold, or null.</returns>
        Task<WaitlistEntry?> GetActiveHoldForClientAsync(string clientUserId,
                                                         DateOnly date,
                                                         TimeOnly startTime,
                                                         Guid employeeId,
                                                         DateTime nowUtc,
                                                         CancellationToken cancellationToken = default);

        /// <summary>The client's non-cancelled waitlist entries, ordered by date/time.</summary>
        /// <param name="clientUserId">Harmony user id of the client.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The client's active (non-cancelled) entries.</returns>
        Task<IReadOnlyList<WaitlistEntry>> GetActiveByClientUserIdAsync(string clientUserId, CancellationToken cancellationToken = default);
    }
}
