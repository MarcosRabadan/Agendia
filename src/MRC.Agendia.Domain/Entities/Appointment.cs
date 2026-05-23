using MRC.Agendia.Domain.Common;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Domain.Entities
{
    public class Appointment : IAuditable, ISoftDelete
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

        public Client Client { get; set; } = null!;
        public Employee Employee { get; set; } = null!;
        public Service Service { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
