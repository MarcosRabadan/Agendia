namespace MRC.Agendia.Domain.Entities
{
    public class CustomTimeSlot
    {
        public int Id { get; set; }
        public int ScheduleOverrideId { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        public ScheduleOverride ScheduleOverride { get; set; } = null!;
    }
}
