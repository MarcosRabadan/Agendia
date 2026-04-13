using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Domain.Services
{
    public class EffectiveSchedule
    {
        public DateOnly Date { get; set; }
        public bool IsOpen { get; set; }
        public string? ClosedReason { get; set; }
        public ScheduleOverrideType? OverrideType { get; set; }
        public List<EffectiveTimeSlot> TimeSlots { get; set; } = new();
    }
}
