namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>The requested waitlist entry does not exist (or is not the caller's). Maps to HTTP 404.</summary>
    public class WaitlistEntryNotFoundException : NotFoundException
    {
        public override string Code => "WAITLIST_ENTRY_NOT_FOUND";

        public WaitlistEntryNotFoundException(int id)
            : base($"No existe ninguna entrada de lista de espera con el identificador {id}.")
        {
        }
    }
}
