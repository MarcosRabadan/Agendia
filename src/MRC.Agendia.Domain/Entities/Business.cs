using MRC.Agendia.Domain.Common;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Domain.Entities
{
    public class Business : AuditableEntity
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Whether the business currently accepts bookings. This is scheduling state
        /// (not profile data): the business' public profile - name, address, contact -
        /// lives in the management/identity service, not in Agendia.
        /// </summary>
        public bool IsActive { get; set; }

        public string? OwnerUserId { get; set; }

        /// <summary>
        /// Minimum advance notice, in hours, a client must give to cancel or
        /// reschedule their own appointment through self-service. Null means no
        /// restriction (the only way to disable it via the API, which otherwise
        /// accepts 1..8760). Staff are never subject to this window.
        /// </summary>
        public int? CancellationWindowHours { get; set; }

        /// <summary>
        /// Two-letter language code (see <see cref="SupportedLanguages"/>) the
        /// business's notifications are delivered in. Defaults to Spanish; travels in
        /// the integration events so the downstream consumer picks the localized
        /// template.
        /// </summary>
        public string DefaultLanguage { get; set; } = SupportedLanguages.Spanish;

        /// <summary>
        /// Initial status applied to new appointments of this business (Pending or
        /// Confirmed). Staff may override it per booking; clients always get this default.
        /// </summary>
        public AppointmentStatus DefaultAppointmentStatus { get; set; } = AppointmentStatus.Pending;

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<Service> Services { get; set; } = new List<Service>();
        public ICollection<ScheduleTemplate> ScheduleTemplates { get; set; } = new List<ScheduleTemplate>();
        public ICollection<ScheduleOverride> ScheduleOverrides { get; set; } = new List<ScheduleOverride>();
    }
}
