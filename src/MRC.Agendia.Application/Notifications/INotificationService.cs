namespace MRC.Agendia.Application.Notifications
{
    /// <summary>
    /// Sends appointment notifications to the client. Best-effort: a delivery
    /// failure is logged and never propagated, so it cannot break the booking
    /// flow. Email is implemented today (push/FCM is tracked separately).
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends the appointment confirmation to the client. Returns true if the
        /// notification was handled (sent, or nothing to do such as a missing
        /// recipient), and false on an unexpected failure so the caller can decide
        /// whether to retry (e.g. the reminder job).
        /// </summary>
        /// <param name="appointmentId">Id of the appointment to notify about.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if handled (sent or nothing to do); false on an unexpected failure.</returns>
        Task<bool> SendAppointmentConfirmationAsync(int appointmentId, CancellationToken cancellationToken = default);

        /// <summary>Sends the 24h reminder to the client.</summary>
        /// <param name="appointmentId">Id of the appointment to remind about.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if handled (sent or nothing to do); false on an unexpected failure.</returns>
        Task<bool> SendAppointmentReminderAsync(int appointmentId, CancellationToken cancellationToken = default);

        /// <summary>Sends the cancellation notice to the client.</summary>
        /// <param name="appointmentId">Id of the appointment that was cancelled.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if handled (sent or nothing to do); false on an unexpected failure.</returns>
        Task<bool> SendAppointmentCancellationAsync(int appointmentId, CancellationToken cancellationToken = default);

        /// <summary>Tells the client the business is running ~<paramref name="delayMinutes"/> minutes late.</summary>
        /// <param name="appointmentId">Id of the affected appointment.</param>
        /// <param name="delayMinutes">How many minutes late the business is running.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if handled (sent or nothing to do); false on an unexpected failure.</returns>
        Task<bool> SendDelayNotificationAsync(int appointmentId, int delayMinutes, CancellationToken cancellationToken = default);

        /// <summary>Tells a waiting client that a slot they were waiting for has freed up.</summary>
        /// <param name="waitlistEntryId">Id of the waitlist entry to notify.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if handled (sent or nothing to do); false on an unexpected failure.</returns>
        Task<bool> SendWaitlistAvailabilityAsync(int waitlistEntryId, CancellationToken cancellationToken = default);
    }
}
