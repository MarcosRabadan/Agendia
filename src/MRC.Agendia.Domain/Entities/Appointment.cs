using MRC.Agendia.Domain.Common;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Domain.Entities
{
    public class Appointment : AuditableEntity
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int EmployeeId { get; set; }
        public int ServiceId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public AppointmentStatus Status { get; set; }
        public string? Notes { get; set; }

        /// <summary>
        /// When the 24h reminder email was sent, or null if not yet sent. Used by
        /// the reminder background job to avoid sending duplicate reminders.
        /// </summary>
        public DateTime? ReminderSentAt { get; set; }

        /// <summary>
        /// Groups appointments generated together as a recurring series (e.g.
        /// "every Friday at 16h"). Null for one-off appointments. Lets a whole
        /// series be cancelled, moved or deleted as a unit.
        /// </summary>
        public Guid? SeriesId { get; set; }

        public Client Client { get; set; } = null!;
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
