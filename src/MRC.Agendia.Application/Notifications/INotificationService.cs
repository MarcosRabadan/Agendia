namespace MRC.Agendia.Application.Notifications
{
    /// <summary>
    /// Sends appointment notifications to the client. Best-effort: a delivery
    /// failure is logged and never propagated, so it cannot break the booking
    /// flow. Email is implemented today (push/FCM is tracked separately).
    /// </summary>
    public interface INotificationService
    {
        Task SendAppointmentConfirmationAsync(int appointmentId, CancellationToken cancellationToken = default);
        Task SendAppointmentReminderAsync(int appointmentId, CancellationToken cancellationToken = default);
        Task SendAppointmentCancellationAsync(int appointmentId, CancellationToken cancellationToken = default);
    }
}
