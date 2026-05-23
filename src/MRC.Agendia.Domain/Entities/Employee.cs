using MRC.Agendia.Domain.Common;

namespace MRC.Agendia.Domain.Entities
{
    public class Employee : IAuditable, ISoftDelete
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }
        public string FullName { get; set; } = null!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
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

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
