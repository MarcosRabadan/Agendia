using MRC.Agendia.Domain.Common;

namespace MRC.Agendia.Domain.Entities
{
    public class Client : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }
        public string? UserId { get; set; }
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
