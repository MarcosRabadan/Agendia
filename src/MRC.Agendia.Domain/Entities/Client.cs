namespace MRC.Agendia.Domain.Entities
{
    public class Client
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
