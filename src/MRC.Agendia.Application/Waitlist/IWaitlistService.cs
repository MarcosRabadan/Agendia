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
        Task<WaitlistEntryDto> JoinAsync(JoinWaitlistDto dto, string userId, CancellationToken cancellationToken = default);
        Task LeaveAsync(int entryId, string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<WaitlistEntryDto>> GetMineAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Best-effort: when a slot frees, notify the first waiting client (FIFO).
        /// Swallows its own errors so it never breaks the cancellation/deletion.
        /// </summary>
        Task NotifyForFreedAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default);
    }
}
