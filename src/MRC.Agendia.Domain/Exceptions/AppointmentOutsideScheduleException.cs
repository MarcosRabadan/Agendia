namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>
    /// The appointment falls on a closed day or outside the open time slots
    /// (e.g. crosses a split-shift break). Maps to HTTP 400.
    /// </summary>
    public class AppointmentOutsideScheduleException : DomainException
    {
        public override string Code => "APPOINTMENT_OUTSIDE_SCHEDULE";

        public AppointmentOutsideScheduleException(string message) : base(message)
        {
        }
    }
}
