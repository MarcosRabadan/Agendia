namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>
    /// The slot still has free capacity, so the client should book directly instead
    /// of joining the waitlist. Maps to HTTP 400.
    /// </summary>
    public class SlotHasCapacityException : DomainException
    {
        public override string Code => "SLOT_HAS_CAPACITY";

        public SlotHasCapacityException()
            : base("La franja todavia tiene hueco disponible; reserva directamente.")
        {
        }
    }
}
