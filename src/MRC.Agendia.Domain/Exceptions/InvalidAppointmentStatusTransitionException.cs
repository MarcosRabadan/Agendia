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
            : base($"No se puede cambiar el estado de una cita de '{from}' a '{to}': '{from}' es un estado final.")
        {
        }
    }
}
