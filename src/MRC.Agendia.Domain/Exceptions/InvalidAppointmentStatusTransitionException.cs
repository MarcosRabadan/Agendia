using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>
    /// Attempted to change an appointment's status out of a terminal state
    /// (Completed/NoShow/Cancelled). Those states are final. Maps to HTTP 400.
    /// </summary>
    public class InvalidAppointmentStatusTransitionException : DomainException
    {
        public override string Code => "INVALID_APPOINTMENT_STATUS_TRANSITION";

        public InvalidAppointmentStatusTransitionException(AppointmentStatus from, AppointmentStatus to)
            : base($"Cannot change an appointment status from '{from}' to '{to}': '{from}' is a final state.")
        {
        }
    }
}
