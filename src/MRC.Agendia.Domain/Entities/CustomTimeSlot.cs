namespace MRC.Agendia.Domain.Entities
{
    public class CustomTimeSlot
    {
        public Guid Id { get; set; }
        public Guid ScheduleOverrideId { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        public ScheduleOverride ScheduleOverride { get; set; } = null!;
    }
}
