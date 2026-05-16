namespace MRC.Agendia.Domain.Entities
{
    public class Employee
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
    }
}
