namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>
    /// The appointment length does not match the service duration. Maps to HTTP 400.
    /// </summary>
    public class AppointmentDurationMismatchException : DomainException
    {
        public override string Code => "APPOINTMENT_DURATION_MISMATCH";

        public AppointmentDurationMismatchException(string message) : base(message)
        {
        }
    }
}
