namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>The requested waitlist entry does not exist (or is not the caller's). Maps to HTTP 404.</summary>
    public class WaitlistEntryNotFoundException : NotFoundException
    {
        public override string Code => "WAITLIST_ENTRY_NOT_FOUND";

        public WaitlistEntryNotFoundException(Guid id)
            : base($"No waitlist entry exists with identifier {id}.")
        {
        }
    }
}
