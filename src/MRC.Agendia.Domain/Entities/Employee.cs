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

        public Business Business { get; set; } = null!;
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
