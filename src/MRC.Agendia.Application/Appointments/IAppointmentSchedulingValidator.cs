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
        /// <summary>
        /// Validates the appointment against the business schedule and existing
        /// bookings, throwing a descriptive exception on the first rule that fails.
        /// </summary>
        /// <param name="appointmentId">
        /// Null when validating a new appointment, set to the existing id
        /// when validating an update (so it is excluded from the conflict check).
        /// </param>
        /// <param name="clientId">Id of the client the appointment is for.</param>
        /// <param name="employeeId">Id of the employee that will attend it (must be active).</param>
        /// <param name="serviceId">Id of the primary service being booked.</param>
        /// <param name="startDate">Wall-clock start of the appointment; cannot be in the past.</param>
        /// <param name="endDate">Wall-clock end of the appointment; must be after the start.</param>
        /// <param name="extraServiceIds">
        /// Optional additional services booked in the same visit (#170). The
        /// appointment's duration must equal the sum of the primary service plus
        /// all of these; each must belong to the same business.
        /// </param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task EnsureValidAsync(int? appointmentId,
                              int clientId,
                              int employeeId,
                              int serviceId,
                              DateTime startDate,
                              DateTime endDate,
                              IReadOnlyCollection<int>? extraServiceIds = null,
                              CancellationToken cancellationToken = default);
    }
}
