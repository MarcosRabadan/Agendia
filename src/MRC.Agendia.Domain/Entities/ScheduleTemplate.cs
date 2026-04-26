using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Domain.Entities
{
    public class ScheduleTemplate
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }
        public string Name { get; set; } = null!;
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly EffectiveTo { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }

        public Business Business { get; set; } = null!;
        public ICollection<WeeklyTimeSlot> WeeklySlots { get; set; } = new List<WeeklyTimeSlot>();
    }
}
