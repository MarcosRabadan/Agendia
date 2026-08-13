using MRC.Agendia.Domain.Common;

namespace MRC.Agendia.Domain.Entities
{
    // Scheduling projection of a business' service. Agendia keeps only the duration,
    // which it needs to lay out availability and validate the appointment interval;
    // the catalog (name, description, price) lives in the management/catalog service.
    public class Service : AuditableEntity
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }
        public int DurationMinutes { get; set; }

        public Business Business { get; set; } = null!;
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
