namespace MRC.Agendia.Domain.Entities
{
    public class BusinessSchedule
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }
        public int DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsWorkingDay { get; set; }

        public Business Business { get; set; } = null!;
    }
}
