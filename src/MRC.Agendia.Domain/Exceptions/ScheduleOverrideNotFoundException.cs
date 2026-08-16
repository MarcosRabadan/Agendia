namespace MRC.Agendia.Domain.Exceptions
{
    public class ScheduleOverrideNotFoundException : NotFoundException
    {
        public override string Code => "SCHEDULE_OVERRIDE_NOT_FOUND";

        public ScheduleOverrideNotFoundException(Guid id) : base($"Schedule override with Id {id} not found.")
        {
        }
    }
}
