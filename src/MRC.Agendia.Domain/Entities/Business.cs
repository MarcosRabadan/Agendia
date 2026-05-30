using MRC.Agendia.Domain.Common;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Domain.Entities
{
    public class Business : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Address { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Email { get; set; } = null!;
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
        /// business sends its notifications in. Defaults to Spanish; selects the
        /// localized email/push templates.
        /// </summary>
        public string DefaultLanguage { get; set; } = SupportedLanguages.Spanish;

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<Service> Services { get; set; } = new List<Service>();
        public ICollection<ScheduleTemplate> ScheduleTemplates { get; set; } = new List<ScheduleTemplate>();
        public ICollection<ScheduleOverride> ScheduleOverrides { get; set; } = new List<ScheduleOverride>();
    }
}
