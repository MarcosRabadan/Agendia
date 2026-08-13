using Microsoft.Extensions.Logging;
using MRC.Agendia.Application.Notifications;

namespace MRC.Agendia.Infrastructure.Notifications
{
    /// <summary>
    /// Temporary no-op implementation of <see cref="INotificationService"/>.
    ///
    /// Delivering a notification needs the recipient's contact details (email,
    /// display name), which used to live on the Client entity. That entity has been
    /// removed (Agendia now stores only the client's Harmony user id), so Agendia can
    /// no longer resolve a recipient on its own. Until notification delivery is moved
    /// to domain events consumed by a dedicated service (#246), every call is logged
    /// and reported as handled (returns <c>true</c>) so callers such as the reminder
    /// job and the waitlist trigger do not treat it as a transient failure and retry
    /// forever. The email/push delivery infrastructure is kept registered but idle
    /// and will be removed together with this class when events land.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<bool> SendAppointmentConfirmationAsync(int appointmentId, CancellationToken cancellationToken = default)
            => SuppressedForAppointment("confirmation", appointmentId);

        /// <inheritdoc />
        public Task<bool> SendAppointmentReminderAsync(int appointmentId, CancellationToken cancellationToken = default)
            => SuppressedForAppointment("reminder", appointmentId);

        /// <inheritdoc />
        public Task<bool> SendAppointmentCancellationAsync(int appointmentId, CancellationToken cancellationToken = default)
            => SuppressedForAppointment("cancellation", appointmentId);

        /// <inheritdoc />
        public Task<bool> SendDelayNotificationAsync(int appointmentId, int delayMinutes, CancellationToken cancellationToken = default)
            => SuppressedForAppointment("delay", appointmentId);

        /// <inheritdoc />
        public Task<bool> SendWaitlistAvailabilityAsync(int waitlistEntryId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Notificacion 'waitlist' para la entrada {Id} omitida: pendiente de la migracion a eventos (#246).",
                waitlistEntryId);
            return Task.FromResult(true);
        }

        private Task<bool> SuppressedForAppointment(string kind, int appointmentId)
        {
            _logger.LogInformation(
                "Notificacion '{Kind}' para la cita {Id} omitida: pendiente de la migracion a eventos (#246).",
                kind, appointmentId);
            return Task.FromResult(true);
        }
    }
}
