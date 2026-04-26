using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Domain.Entities
{
    public class HolidayCalendar
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public string Name { get; set; } = null!;
        public HolidayScope Scope { get; set; }
        public int Year { get; set; }
    }
}
