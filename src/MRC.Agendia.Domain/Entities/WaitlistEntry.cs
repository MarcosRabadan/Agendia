using MRC.Agendia.Domain.Common;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Domain.Entities
{
    /// <summary>
    /// A client's request to be notified when a full slot frees up. The client is
    /// notified (FIFO by <see cref="CreatedAt"/>) and gets a PRIORITY HOLD until
    /// <see cref="HoldUntil"/> (#268), but they still book it themselves - there is no
    /// auto-booking.
    ///
    /// <para><b>The hold reserves ONE seat, not the slot.</b> Where the slot has more free
    /// seats than holds - a group class, or several employees - the rest stay bookable by
    /// anyone. Reading it as a block on the whole slot is what made availability hide free
    /// seats and refuse bookings it had just offered (#308).</para>
    ///
    /// <see cref="EmployeeId"/> null means "any employee": that seat is held on the
    /// business as a whole rather than on one person.
    /// </summary>
    public class WaitlistEntry : Entity
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid ServiceId { get; set; }

        /// <summary>
        /// Harmony user id (the JWT <c>sub</c>) of the waiting client. The client
        /// identity lives in Harmony; only its opaque id is stored here (no FK).
        /// </summary>
        public string ClientUserId { get; set; } = null!;

        public Guid? EmployeeId { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public WaitlistStatus Status { get; set; }

        /// <summary>UTC creation instant; drives the FIFO order of notifications.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// While the client is Notified, the UTC instant their priority hold on the slot
        /// runs out (#268). Nobody else can book the slot until then; once it passes, the
        /// expiry job moves the queue on. Null for an entry that was never notified.
        /// </summary>
        public DateTime? HoldUntil { get; set; }

        public Service Service { get; set; } = null!;
    }
}
