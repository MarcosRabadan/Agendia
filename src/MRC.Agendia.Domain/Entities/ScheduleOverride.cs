using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Domain.Entities
{
    public class ScheduleOverride
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public DateOnly Date { get; set; }
        public ScheduleOverrideType OverrideType { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }

        public Business Business { get; set; } = null!;
        public ICollection<CustomTimeSlot> CustomSlots { get; set; } = new List<CustomTimeSlot>();
    }
}
