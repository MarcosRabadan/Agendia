namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>
    /// The slot is being held for another client from the waitlist (#268), so it cannot
    /// be booked by anyone else until the hold runs out. Maps to HTTP 400.
    /// </summary>
    public class SlotOnHoldException : DomainException
    {
        public override string Code => "SLOT_ON_HOLD";

        public SlotOnHoldException()
            : base("That slot is reserved for a client from the waitlist for a few more minutes.")
        {
        }
    }
}
