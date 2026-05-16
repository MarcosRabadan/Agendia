namespace MRC.Agendia.Application.Appointments
{
    /// <summary>
    /// Validates that an appointment is allowed against the business schedule
    /// (open hours, holidays, split shifts) and against other appointments
    /// (no employee double-booking beyond their capacity, duration matches
    /// the service).
    ///
    /// Throws <see cref="InvalidOperationException"/> with a descriptive
    /// message on every business-rule failure. The exception is mapped to
    /// HTTP 400 by the global error middleware.
    /// </summary>
    public interface IAppointmentSchedulingValidator
    {
        /// <param name="appointmentId">
        /// Null when validating a new appointment, set to the existing id
        /// when validating an update (so it is excluded from the conflict check).
        /// </param>
        Task EnsureValidAsync(
            int? appointmentId,
            int clientId,
            int employeeId,
            int serviceId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);
    }
}
