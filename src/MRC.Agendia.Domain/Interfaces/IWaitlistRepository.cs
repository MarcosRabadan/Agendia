using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IWaitlistRepository
    {
        /// <summary>Gets a tracked waitlist entry by id.</summary>
        /// <param name="id">Waitlist entry id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The entry, or null when missing.</returns>
        Task<WaitlistEntry?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Adds a new waitlist entry to the context.</summary>
        /// <param name="entry">The entry to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task AddAsync(WaitlistEntry entry, CancellationToken cancellationToken = default);

        /// <summary>Marks a waitlist entry as modified.</summary>
        /// <param name="entry">The entry to update.</param>
        void Update(WaitlistEntry entry);

        /// <summary>Loads the entry with its (live) Client and Service for composing the notification.</summary>
        /// <param name="id">Waitlist entry id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The untracked entry with details, or null when missing or a participant is soft-deleted.</returns>
        Task<WaitlistEntry?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>True if the client already has a Waiting entry for this exact slot.</summary>
        /// <param name="clientId">Client id.</param>
        /// <param name="businessId">Business id.</param>
        /// <param name="serviceId">Service id.</param>
        /// <param name="date">Slot date.</param>
        /// <param name="startTime">Slot start time.</param>
        /// <param name="employeeId">Requested employee id, or null for "any employee".</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True when a matching Waiting entry already exists.</returns>
        Task<bool> ExistsWaitingAsync(
            int clientId, int businessId, int serviceId, DateOnly date, TimeOnly startTime, int? employeeId,
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
            int businessId, int serviceId, DateOnly date, TimeOnly startTime, int employeeId,
            CancellationToken cancellationToken = default);

        /// <summary>The client's non-cancelled waitlist entries, ordered by date/time.</summary>
        /// <param name="clientId">Client id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The client's active (non-cancelled) entries.</returns>
        Task<IReadOnlyList<WaitlistEntry>> GetActiveByClientAsync(int clientId, CancellationToken cancellationToken = default);
    }
}
