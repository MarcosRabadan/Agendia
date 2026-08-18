namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>The business already has a default schedule template. Maps to HTTP 400.</summary>
    public class DuplicateDefaultScheduleTemplateException : DomainException
    {
        public override string Code => "DUPLICATE_DEFAULT_SCHEDULE_TEMPLATE";

        public DuplicateDefaultScheduleTemplateException()
            : base("The business already has a default schedule template.")
        {
        }
    }
}
