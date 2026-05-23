namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>
    /// The appointment dates are missing, inverted, or in the past. Maps to HTTP 400.
    /// </summary>
    public class InvalidAppointmentTimeException : DomainException
    {
        public override string Code => "INVALID_APPOINTMENT_TIME";

        public InvalidAppointmentTimeException(string message) : base(message)
        {
        }
    }
}
