namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>
    /// Booking would exceed the employee's MaxConcurrentAppointments at that
    /// time. Maps to HTTP 400.
    /// </summary>
    public class AppointmentConflictException : DomainException
    {
        public override string Code => "APPOINTMENT_CONFLICT";

        public AppointmentConflictException(string message) : base(message)
        {
        }
    }
}
