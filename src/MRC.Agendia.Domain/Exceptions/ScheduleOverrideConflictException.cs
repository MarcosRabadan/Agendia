namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>Another schedule override already exists for that business and date. Maps to HTTP 400.</summary>
    public class ScheduleOverrideConflictException : DomainException
    {
        public override string Code => "SCHEDULE_OVERRIDE_CONFLICT";

        public ScheduleOverrideConflictException(string message) : base(message)
        {
        }
    }
}
