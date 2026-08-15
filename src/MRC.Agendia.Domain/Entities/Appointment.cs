using MRC.Agendia.Domain.Common;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Domain.Entities
{
    public class Appointment : AuditableEntity
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Harmony user id (the JWT <c>sub</c>) of the client who booked. Agendia no
        /// longer owns a Client profile: the identity lives in Harmony and only its
        /// opaque id is stored here. Not a foreign key (no cross-service FK).
        /// </summary>
        public string ClientUserId { get; set; } = null!;

        public Guid EmployeeId { get; set; }
        public Guid ServiceId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public AppointmentStatus Status { get; set; }
        public string? Notes { get; set; }

        /// <summary>
        /// When the 24h reminder event was published, or null if not yet. Used by
        /// the reminder background job to avoid publishing duplicate reminders.
        /// </summary>
        public DateTime? ReminderSentAt { get; set; }

        /// <summary>
        /// Groups appointments generated together as a recurring series (e.g.
        /// "every Friday at 16h"). Null for one-off appointments. Lets a whole
        /// series be cancelled, moved or deleted as a unit.
        /// </summary>
        public Guid? SeriesId { get; set; }

        public Employee Employee { get; set; } = null!;
        public Service Service { get; set; } = null!;

        /// <summary>
        /// Additional services booked in the same visit beyond the primary
        /// <see cref="ServiceId"/>. Empty for single-service appointments. The
        /// total duration/price is the primary service plus all of these.
        /// </summary>
        public ICollection<AppointmentExtraService> ExtraServices { get; set; } = new List<AppointmentExtraService>();
    }
}
