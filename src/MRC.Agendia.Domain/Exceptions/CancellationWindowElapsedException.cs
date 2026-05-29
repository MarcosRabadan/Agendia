namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>
    /// A client tried to cancel or reschedule their own appointment through
    /// self-service after the business's advance-notice window had elapsed
    /// (the appointment is too close to its start time). Maps to HTTP 400.
    /// </summary>
    public class CancellationWindowElapsedException : DomainException
    {
        public override string Code => "CANCELLATION_WINDOW_ELAPSED";

        public CancellationWindowElapsedException(int windowHours)
            : base($"No puedes cancelar ni reprogramar la cita con menos de {windowHours}h de antelacion. Contacta con el negocio.")
        {
        }
    }
}
