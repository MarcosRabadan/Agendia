using MRC.Agendia.Domain.Common;

namespace MRC.Agendia.Domain.Entities
{
    // A bookable resource of a business (a person, a room, a chair...). Agendia holds
    // only its scheduling attributes; the profile (name, contact) lives in the
    // management/identity service, keyed by UserId when the resource has an account.
    public class Employee : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public bool IsActive { get; set; }
        public string? UserId { get; set; }

        /// <summary>
        /// How many simultaneous appointments this employee can hold.
        /// Default 1 (one-to-one service).
        /// Use higher values for:
        ///  - Hair stylists working multiple clients in parallel (e.g. dye + cut)
        ///  - Group instructors (yoga, music, fitness)
        ///  - Any other resource that serves several clients at once.
        /// </summary>
        public int MaxConcurrentAppointments { get; set; } = 1;

        public Business Business { get; set; } = null!;
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
