using MRC.Agendia.Domain.Exceptions;

namespace MRC.Agendia.Application.Appointments
{
    /// <summary>
    /// Self-service cancellation/reschedule window rule. A business may require a
    /// minimum advance notice (<c>CancellationWindowHours</c>) before a client can
    /// cancel or reschedule their own appointment. Staff are not subject to this
    /// rule. Pure and side-effect free so it can be unit-tested in isolation.
    /// </summary>
    public static class AppointmentCancellationPolicy
    {
        /// <summary>
        /// Throws <see cref="CancellationWindowElapsedException"/> when self-service
        /// cancellation/reschedule is no longer allowed because the appointment is
        /// within the business's advance-notice window. A null or non-positive
        /// window means no restriction.
        /// </summary>
        /// <param name="appointmentStart">Wall-clock start of the appointment being cancelled/moved.</param>
        /// <param name="windowHours">The business's configured window, or null for no restriction.</param>
        /// <param name="now">Current business wall-clock time.</param>
        public static void EnsureSelfServiceAllowed(DateTime appointmentStart, int? windowHours, DateTime now)
        {
            if (windowHours is null || windowHours.Value <= 0)
                return;

            // Latest moment the client may still act on their own. Past it, only staff can.
            var deadline = appointmentStart.AddHours(-windowHours.Value);
            if (now > deadline)
                throw new CancellationWindowElapsedException(windowHours.Value);
        }
    }
}
