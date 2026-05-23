using MRC.Agendia.Domain.Common;

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

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<Service> Services { get; set; } = new List<Service>();
        public ICollection<ScheduleTemplate> ScheduleTemplates { get; set; } = new List<ScheduleTemplate>();
        public ICollection<ScheduleOverride> ScheduleOverrides { get; set; } = new List<ScheduleOverride>();
    }
}
