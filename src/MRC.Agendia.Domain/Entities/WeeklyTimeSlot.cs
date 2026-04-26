using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Domain.Entities
{
    public class WeeklyTimeSlot
    {
        public int Id { get; set; }
        public int ScheduleTemplateId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public TimeSlotType SlotType { get; set; }

        public ScheduleTemplate ScheduleTemplate { get; set; } = null!;
    }
}
