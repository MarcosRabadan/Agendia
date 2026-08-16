namespace MRC.Agendia.Domain.Exceptions
{
    public class ScheduleTemplateNotFoundException : NotFoundException
    {
        public override string Code => "SCHEDULE_TEMPLATE_NOT_FOUND";

        public ScheduleTemplateNotFoundException(Guid id) : base($"Schedule template with Id {id} not found.")
        {
        }
    }
}
