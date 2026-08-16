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
        /// The first (FIFO by CreatedAt) Waiting entry that matches a freed slot:
        /// same business/service/date/time and either "any employee" or that employee.
        /// Tracked so the caller can mark it Notified.
        /// </summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="serviceId">Service id.</param>
        /// <param name="date">Freed slot date.</param>
        /// <param name="startTime">Freed slot start time.</param>
        /// <param name="employeeId">Id of the employee whose slot was freed.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The next matching Waiting entry (excluding soft-deleted participants), or null when none.</returns>
        Task<WaitlistEntry?> GetNextWaitingForSlotAsync(
            Guid businessId, Guid serviceId, DateOnly date, TimeOnly startTime, Guid employeeId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The active priority holds (#268) of a business on a date: entries Notified whose
        /// HoldUntil has not passed. Untracked - the availability read only counts them.
        /// </summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="date">Slot date (wall clock).</param>
        /// <param name="nowUtc">Current UTC instant, to drop the expired ones.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The entries still holding a slot that day.</returns>
        Task<IReadOnlyList<WaitlistEntry>> GetActiveHoldsAsync(Guid businessId,
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
