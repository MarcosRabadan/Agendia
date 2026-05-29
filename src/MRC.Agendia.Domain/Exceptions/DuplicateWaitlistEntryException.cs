namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>The client is already on the waitlist for this slot. Maps to HTTP 400.</summary>
    public class DuplicateWaitlistEntryException : DomainException
    {
        public override string Code => "DUPLICATE_WAITLIST_ENTRY";

        public DuplicateWaitlistEntryException()
            : base("Ya estas en la lista de espera de esta franja.")
        {
        }
    }
}
