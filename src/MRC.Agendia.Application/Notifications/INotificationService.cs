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
        /// Returns true if the notification was handled (sent, or nothing to do
        /// such as a missing recipient), and false on an unexpected failure so the
        /// caller can decide whether to retry (e.g. the reminder job).
        /// </summary>
        Task<bool> SendAppointmentConfirmationAsync(int appointmentId, CancellationToken cancellationToken = default);
        Task<bool> SendAppointmentReminderAsync(int appointmentId, CancellationToken cancellationToken = default);
        Task<bool> SendAppointmentCancellationAsync(int appointmentId, CancellationToken cancellationToken = default);

        /// <summary>Tells the client the business is running ~<paramref name="delayMinutes"/> minutes late.</summary>
        Task<bool> SendDelayNotificationAsync(int appointmentId, int delayMinutes, CancellationToken cancellationToken = default);
    }
}
