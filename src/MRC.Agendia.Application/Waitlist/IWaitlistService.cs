using MRC.Agendia.Application.Waitlist.DTO;

namespace MRC.Agendia.Application.Waitlist
{
    /// <summary>
    /// Waitlist for full slots: a client joins, lists or leaves their entries, and
    /// when a slot frees (an appointment is cancelled/deleted) the first waiting
    /// client is notified (FIFO). The client books manually; there is no auto-booking.
    /// </summary>
    public interface IWaitlistService
    {
        /// <summary>Adds the calling client to the waitlist for a full slot.</summary>
        /// <param name="dto">Slot to wait for (business, service, date, start time, optional employee).</param>
        /// <param name="userId">Identity user id of the calling client.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The created waitlist entry.</returns>
        Task<WaitlistEntryDto> JoinAsync(JoinWaitlistDto dto, string userId, CancellationToken cancellationToken = default);

        /// <summary>Cancels one of the calling client's own waitlist entries (idempotent).</summary>
        /// <param name="entryId">Id of the waitlist entry to cancel.</param>
        /// <param name="userId">Identity user id of the calling client; must own the entry.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task LeaveAsync(int entryId, string userId, CancellationToken cancellationToken = default);

        /// <summary>Lists the calling client's active (waiting/notified) waitlist entries.</summary>
        /// <param name="userId">Identity user id of the calling client.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The client's active waitlist entries (empty when the user is not a client).</returns>
        Task<IReadOnlyList<WaitlistEntryDto>> GetMineAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Best-effort: when a slot frees, notify the first waiting client (FIFO).
        /// Swallows its own errors so it never breaks the cancellation/deletion.
        /// </summary>
        /// <param name="appointmentId">Id of the appointment that was cancelled/deleted, freeing the slot.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task NotifyForFreedAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default);
    }
}
