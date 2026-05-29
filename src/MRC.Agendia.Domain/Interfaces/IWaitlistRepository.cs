using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IWaitlistRepository
    {
        Task<WaitlistEntry?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task AddAsync(WaitlistEntry entry, CancellationToken cancellationToken = default);
        void Update(WaitlistEntry entry);

        /// <summary>Loads the entry with its (live) Client and Service for composing the notification.</summary>
        Task<WaitlistEntry?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>True if the client already has a Waiting entry for this exact slot.</summary>
        Task<bool> ExistsWaitingAsync(
            int clientId, int businessId, int serviceId, DateOnly date, TimeOnly startTime, int? employeeId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The first (FIFO by CreatedAt) Waiting entry that matches a freed slot:
        /// same business/service/date/time and either "any employee" or that employee.
        /// Tracked so the caller can mark it Notified.
        /// </summary>
        Task<WaitlistEntry?> GetNextWaitingForSlotAsync(
            int businessId, int serviceId, DateOnly date, TimeOnly startTime, int employeeId,
            CancellationToken cancellationToken = default);

        /// <summary>The client's non-cancelled waitlist entries, ordered by date/time.</summary>
        Task<IReadOnlyList<WaitlistEntry>> GetActiveByClientAsync(int clientId, CancellationToken cancellationToken = default);
    }
}
