using System.Net;
using Microsoft.Extensions.Logging;
using MRC.Agendia.Application.Common.Email;
using MRC.Agendia.Application.Common.Push;
using MRC.Agendia.Application.Notifications;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Notifications
{
    /// <summary>
    /// Email implementation of <see cref="INotificationService"/>. Loads the
    /// appointment with its client/service/employee/business and sends a Spanish
    /// HTML email via <see cref="IEmailSender"/>. Best-effort: any failure (no
    /// recipient email, delivery error...) is logged and swallowed so it never
    /// breaks the booking flow.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IWaitlistRepository _waitlistRepository;
        private readonly IDeviceTokenRepository _deviceTokenRepository;
        private readonly IEmailSender _emailSender;
        private readonly IPushSender _pushSender;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IAppointmentRepository appointmentRepository,
            IWaitlistRepository waitlistRepository,
            IDeviceTokenRepository deviceTokenRepository,
            IEmailSender emailSender,
            IPushSender pushSender,
            ILogger<NotificationService> logger)
        {
            _appointmentRepository = appointmentRepository;
            _waitlistRepository = waitlistRepository;
            _deviceTokenRepository = deviceTokenRepository;
            _emailSender = emailSender;
            _pushSender = pushSender;
            _logger = logger;
        }

        public Task<bool> SendAppointmentConfirmationAsync(int appointmentId, CancellationToken cancellationToken = default)
            => SendAsync(appointmentId, "confirmation", BuildConfirmation, cancellationToken);

        public Task<bool> SendAppointmentReminderAsync(int appointmentId, CancellationToken cancellationToken = default)
            => SendAsync(appointmentId, "reminder", BuildReminder, cancellationToken);

        public Task<bool> SendAppointmentCancellationAsync(int appointmentId, CancellationToken cancellationToken = default)
            => SendAsync(appointmentId, "cancellation", BuildCancellation, cancellationToken);

        public Task<bool> SendDelayNotificationAsync(int appointmentId, int delayMinutes, CancellationToken cancellationToken = default)
            => SendAsync(appointmentId, "delay", a => BuildDelay(a, delayMinutes), cancellationToken);

        public async Task<bool> SendWaitlistAvailabilityAsync(int waitlistEntryId, CancellationToken cancellationToken = default)
        {
            try
            {
                var entry = await _waitlistRepository.GetByIdWithDetailsAsync(waitlistEntryId, cancellationToken);
                if (entry is null)
                {
                    _logger.LogWarning("Notification waitlist: entry {Id} not found or participant deleted; skipping.", waitlistEntryId);
                    return true;
                }

                var subject = "Se ha liberado un hueco - Agendia";
                var body =
                    $"<p>Hola {Encode(entry.Client!.Name)},</p>" +
                    "<p>Se ha liberado un hueco que estabas esperando:</p>" +
                    "<ul>" +
                    $"<li><strong>Servicio:</strong> {Encode(entry.Service.Name)}</li>" +
                    $"<li><strong>Fecha:</strong> {entry.Date:dd/MM/yyyy} a las {entry.StartTime:HH:mm}</li>" +
                    "</ul>" +
                    "<p>Reserva cuanto antes desde la app; las plazas se asignan por orden de llegada.</p>";

                // Push first, best-effort and independent of email.
                await TrySendPushAsync(
                    entry.Client?.UserId, subject,
                    $"{entry.Service.Name} - {entry.Date:dd/MM} {entry.StartTime:HH:mm}",
                    new Dictionary<string, string> { ["waitlistEntryId"] = waitlistEntryId.ToString(), ["type"] = "waitlist" },
                    cancellationToken);

                var email = entry.Client?.Email;
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("Notification waitlist: entry {Id} client has no email; skipping email.", waitlistEntryId);
                    return true;
                }

                await _emailSender.SendAsync(email, subject, body);
                _logger.LogInformation("Notification waitlist email sent for entry {Id} to {Email}.", waitlistEntryId, email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification waitlist failed for entry {Id}.", waitlistEntryId);
                return false;
            }
        }

        private async Task<bool> SendAsync(
            int appointmentId,
            string kind,
            Func<Appointment, (string Subject, string Body)> compose,
            CancellationToken cancellationToken)
        {
            try
            {
                var appointment = await _appointmentRepository.GetByIdWithDetailsAsync(appointmentId, cancellationToken);
                if (appointment is null)
                {
                    _logger.LogWarning("Notification {Kind}: appointment {Id} not found.", kind, appointmentId);
                    return true;
                }

                var (subject, body) = compose(appointment);

                // Push first, best-effort and independent of email: a client may have
                // a device registered even without an email on file.
                await TrySendPushAsync(
                    appointment.Client?.UserId, subject, PushSummary(appointment),
                    new Dictionary<string, string> { ["appointmentId"] = appointmentId.ToString(), ["type"] = kind },
                    cancellationToken);

                var email = appointment.Client?.Email;
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning(
                        "Notification {Kind}: appointment {Id} client has no email; skipping email.", kind, appointmentId);
                    return true;
                }

                await _emailSender.SendAsync(email, subject, body);

                _logger.LogInformation(
                    "Notification {Kind} email sent for appointment {Id} to {Email}.", kind, appointmentId, email);
                return true;
            }
            catch (Exception ex)
            {
                // Unexpected/transient failure: report it so callers (e.g. the
                // reminder job) can avoid marking the notification as delivered.
                _logger.LogError(
                    ex, "Notification {Kind} failed for appointment {Id}.", kind, appointmentId);
                return false;
            }
        }

        private static (string Subject, string Body) BuildConfirmation(Appointment a)
        {
            var subject = "Cita confirmada - Agendia";
            var body =
                $"<p>Hola {Encode(a.Client.Name)},</p>" +
                $"<p>Tu cita ha sido confirmada:</p>" +
                Details(a) +
                "<p>Te esperamos. Si necesitas cancelarla, hazlo desde la app.</p>";
            return (subject, body);
        }

        private static (string Subject, string Body) BuildReminder(Appointment a)
        {
            var subject = "Recordatorio de tu cita - Agendia";
            var body =
                $"<p>Hola {Encode(a.Client.Name)},</p>" +
                "<p>Te recordamos tu proxima cita:</p>" +
                Details(a) +
                "<p>Si no puedes asistir, cancelala con antelacion desde la app.</p>";
            return (subject, body);
        }

        private static (string Subject, string Body) BuildCancellation(Appointment a)
        {
            var subject = "Cita cancelada - Agendia";
            var body =
                $"<p>Hola {Encode(a.Client.Name)},</p>" +
                "<p>Tu cita ha sido cancelada:</p>" +
                Details(a) +
                "<p>Puedes reservar una nueva cuando quieras desde la app.</p>";
            return (subject, body);
        }

        private static (string Subject, string Body) BuildDelay(Appointment a, int delayMinutes)
        {
            var subject = "Tu cita va con retraso - Agendia";
            var body =
                $"<p>Hola {Encode(a.Client.Name)},</p>" +
                $"<p>Te avisamos de que vamos con aproximadamente <strong>{delayMinutes} minutos</strong> de retraso sobre tu cita:</p>" +
                Details(a) +
                "<p>Disculpa las molestias. Puedes venir un poco mas tarde sin perder tu turno.</p>";
            return (subject, body);
        }

        private async Task TrySendPushAsync(
            string? userId,
            string title,
            string body,
            IReadOnlyDictionary<string, string> data,
            CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return;

                var tokens = await _deviceTokenRepository.GetTokensByUserIdAsync(userId, cancellationToken);
                if (tokens.Count == 0)
                    return;

                await _pushSender.SendAsync(tokens, title, body, data, cancellationToken);
                _logger.LogInformation(
                    "Notification push sent to {Count} device(s) for user {UserId}.", tokens.Count, userId);
            }
            catch (Exception ex)
            {
                // Push is strictly best-effort: never affect the email result or the booking flow.
                _logger.LogError(ex, "Notification push failed for user {UserId}.", userId);
            }
        }

        private static string PushSummary(Appointment a)
            => $"{a.Service.Name} - {a.StartDate:dd/MM HH:mm}";

        private static string Details(Appointment a)
            => "<ul>" +
               $"<li><strong>Servicio:</strong> {Encode(a.Service.Name)}</li>" +
               $"<li><strong>Profesional:</strong> {Encode(a.Employee.FullName)}</li>" +
               $"<li><strong>Negocio:</strong> {Encode(a.Employee.Business.Name)}</li>" +
               $"<li><strong>Fecha:</strong> {a.StartDate:dd/MM/yyyy HH:mm} - {a.EndDate:HH:mm}</li>" +
               "</ul>";

        private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
