using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Domain.Entities
{
    /// <summary>
    /// A client's request to be notified when a full slot frees up. The client is
    /// notified (FIFO by <see cref="CreatedAt"/>) but books manually; there is no
    /// auto-booking. <see cref="EmployeeId"/> null means "any employee".
    /// </summary>
    public class WaitlistEntry
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }
        public int ServiceId { get; set; }
        public int ClientId { get; set; }
        public int? EmployeeId { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public WaitlistStatus Status { get; set; }

        /// <summary>UTC creation instant; drives the FIFO order of notifications.</summary>
        public DateTime CreatedAt { get; set; }

        public Client Client { get; set; } = null!;
        public Service Service { get; set; } = null!;
    }
}
