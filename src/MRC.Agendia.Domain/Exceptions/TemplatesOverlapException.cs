namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>Two schedule templates cover overlapping date ranges. Maps to HTTP 400.</summary>
    public class TemplatesOverlapException : DomainException
    {
        public override string Code => "SCHEDULE_TEMPLATES_OVERLAP";

        public TemplatesOverlapException(string message) : base(message)
        {
        }
    }
}
