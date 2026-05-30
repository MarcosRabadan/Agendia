using System.Globalization;
using System.Net;
using Microsoft.Extensions.Logging;
using MRC.Agendia.Application.Common.Email;
using MRC.Agendia.Application.Common.Push;
using MRC.Agendia.Application.Notifications;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Infrastructure.Notifications.Resources;

namespace MRC.Agendia.Infrastructure.Notifications
{
    /// <summary>
    /// Email/push implementation of <see cref="INotificationService"/>. Loads the
    /// appointment (or waitlist entry) with its client/service/employee/business and
    /// sends an HTML email via <see cref="IEmailSender"/> plus a best-effort push.
    /// The template language follows the business' <see cref="Business.DefaultLanguage"/>
    /// (es/en/fr); the HTML structure stays here and the phrases come from the
    /// NotificationStrings .resx set, resolved by an explicit culture (there is no
    /// request/CurrentUICulture: sends are async/background). Best-effort: any failure
    /// (no recipient email, delivery error...) is logged and swallowed so it never
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

        public NotificationService(IAppointmentRepository appointmentRepository,
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

        /// <inheritdoc />
        public Task<bool> SendAppointmentConfirmationAsync(int appointmentId, CancellationToken cancellationToken = default)
            => SendAsync(appointmentId, "confirmation", BuildConfirmation, cancellationToken);

        /// <inheritdoc />
        public Task<bool> SendAppointmentReminderAsync(int appointmentId, CancellationToken cancellationToken = default)
            => SendAsync(appointmentId, "reminder", BuildReminder, cancellationToken);

        /// <inheritdoc />
        public Task<bool> SendAppointmentCancellationAsync(int appointmentId, CancellationToken cancellationToken = default)
            => SendAsync(appointmentId, "cancellation", BuildCancellation, cancellationToken);

        /// <inheritdoc />
        public Task<bool> SendDelayNotificationAsync(int appointmentId, int delayMinutes, CancellationToken cancellationToken = default)
            => SendAsync(appointmentId, "delay", (a, culture) => BuildDelay(a, delayMinutes, culture), cancellationToken);

        /// <inheritdoc />
        public async Task<bool> SendWaitlistAvailabilityAsync(int waitlistEntryId, CancellationToken cancellationToken = default)
        {
            WaitlistEntry? entry;
            try
            {
                entry = await _waitlistRepository.GetByIdWithDetailsAsync(waitlistEntryId, cancellationToken);
            }
            catch (Exception ex)
            {
                // Loading failed (DB hiccup): transient, let the caller retry.
                _logger.LogError(ex, "Notification waitlist: loading entry {Id} failed.", waitlistEntryId);
                return false;
            }

            if (entry is null)
            {
                _logger.LogWarning("Notification waitlist: entry {Id} not found or participant deleted; skipping.", waitlistEntryId);
                return true;
            }

            // Compose OUTSIDE the delivery try (see SendAsync): a build error is permanent,
            // reported as handled so the freed-slot trigger does not re-select it forever.
            string subject, body, pushSummary;
            string? clientUserId, email;
            try
            {
                var culture = ResolveCulture(entry.Service.Business?.DefaultLanguage);
                subject = NotificationText.Get("Subject_Waitlist", culture);
                var slot = NotificationText.Format(
                    "Waitlist_SlotFormat", culture,
                    entry.Date.ToString("d", culture),
                    entry.StartTime.ToString("t", culture));
                body =
                    $"<p>{NotificationText.Format("Greeting", culture, Encode(entry.Client!.Name))}</p>" +
                    $"<p>{NotificationText.Get("Waitlist_Intro", culture)}</p>" +
                    "<ul>" +
                    $"<li><strong>{NotificationText.Get("Label_Service", culture)}</strong> {Encode(entry.Service.Name)}</li>" +
                    $"<li><strong>{NotificationText.Get("Label_Date", culture)}</strong> {slot}</li>" +
                    "</ul>" +
                    $"<p>{NotificationText.Get("Waitlist_Outro", culture)}</p>";
                pushSummary = $"{entry.Service.Name} - {entry.Date.ToString("d", culture)} {entry.StartTime.ToString("t", culture)}";
                clientUserId = entry.Client?.UserId;
                email = entry.Client?.Email;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification waitlist: building the message for entry {Id} failed.", waitlistEntryId);
                return true;
            }

            // Push first, best-effort and independent of email.
            await TrySendPushAsync(
                clientUserId, subject, pushSummary,
                new Dictionary<string, string> { ["waitlistEntryId"] = waitlistEntryId.ToString(), ["type"] = "waitlist" },
                cancellationToken);

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Notification waitlist: entry {Id} client has no email; skipping email.", waitlistEntryId);
                return true;
            }

            try
            {
                await _emailSender.SendAsync(email, subject, body, cancellationToken);
                _logger.LogInformation("Notification waitlist email sent for entry {Id} to {Email}.", waitlistEntryId, email);
                return true;
            }
            catch (Exception ex)
            {
                // Delivery failure is transient: report it so the trigger can retry later.
                _logger.LogError(ex, "Notification waitlist: sending email for entry {Id} failed.", waitlistEntryId);
                return false;
            }
        }

        private async Task<bool> SendAsync(int appointmentId,
                                           string kind,
                                           Func<Appointment, CultureInfo, (string Subject, string Body)> compose,
                                           CancellationToken cancellationToken)
        {
            Appointment? appointment;
            try
            {
                appointment = await _appointmentRepository.GetByIdWithDetailsAsync(appointmentId, cancellationToken);
            }
            catch (Exception ex)
            {
                // Loading failed (DB hiccup): transient, let the caller retry.
                _logger.LogError(ex, "Notification {Kind}: loading appointment {Id} failed.", kind, appointmentId);
                return false;
            }

            if (appointment is null)
            {
                _logger.LogWarning("Notification {Kind}: appointment {Id} not found.", kind, appointmentId);
                return true;
            }

            // Compose OUTSIDE the delivery try. A build error (e.g. a required
            // navigation is missing) is a permanent data problem, not a transient
            // delivery failure, so it is reported as handled (return true) to avoid the
            // reminder job retrying it forever - and logged so it can be triaged.
            string subject, body, pushSummary;
            string? clientUserId, email;
            try
            {
                var culture = ResolveCulture(appointment.Employee?.Business?.DefaultLanguage);
                (subject, body) = compose(appointment, culture);
                pushSummary = PushSummary(appointment, culture);
                clientUserId = appointment.Client?.UserId;
                email = appointment.Client?.Email;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification {Kind}: building the message for appointment {Id} failed.", kind, appointmentId);
                return true;
            }

            // Push first, best-effort and independent of email: a client may have
            // a device registered even without an email on file.
            await TrySendPushAsync(
                clientUserId, subject, pushSummary,
                new Dictionary<string, string> { ["appointmentId"] = appointmentId.ToString(), ["type"] = kind },
                cancellationToken);

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning(
                    "Notification {Kind}: appointment {Id} client has no email; skipping email.", kind, appointmentId);
                return true;
            }

            try
            {
                await _emailSender.SendAsync(email, subject, body, cancellationToken);
                _logger.LogInformation(
                    "Notification {Kind} email sent for appointment {Id} to {Email}.", kind, appointmentId, email);
                return true;
            }
            catch (Exception ex)
            {
                // Delivery failure is transient: report it so callers (e.g. the reminder
                // job) avoid marking the notification as delivered and retry later.
                _logger.LogError(ex, "Notification {Kind}: sending email for appointment {Id} failed.", kind, appointmentId);
                return false;
            }
        }

        private static (string Subject, string Body) BuildConfirmation(Appointment a, CultureInfo culture)
        {
            var subject = NotificationText.Get("Subject_Confirmation", culture);
            var body =
                $"<p>{NotificationText.Format("Greeting", culture, Encode(a.Client.Name))}</p>" +
                $"<p>{NotificationText.Get("Confirmation_Intro", culture)}</p>" +
                Details(a, culture) +
                $"<p>{NotificationText.Get("Confirmation_Outro", culture)}</p>";
            return (subject, body);
        }

        private static (string Subject, string Body) BuildReminder(Appointment a, CultureInfo culture)
        {
            var subject = NotificationText.Get("Subject_Reminder", culture);
            var body =
                $"<p>{NotificationText.Format("Greeting", culture, Encode(a.Client.Name))}</p>" +
                $"<p>{NotificationText.Get("Reminder_Intro", culture)}</p>" +
                Details(a, culture) +
                $"<p>{NotificationText.Get("Reminder_Outro", culture)}</p>";
            return (subject, body);
        }

        private static (string Subject, string Body) BuildCancellation(Appointment a, CultureInfo culture)
        {
            var subject = NotificationText.Get("Subject_Cancellation", culture);
            var body =
                $"<p>{NotificationText.Format("Greeting", culture, Encode(a.Client.Name))}</p>" +
                $"<p>{NotificationText.Get("Cancellation_Intro", culture)}</p>" +
                Details(a, culture) +
                $"<p>{NotificationText.Get("Cancellation_Outro", culture)}</p>";
            return (subject, body);
        }

        private static (string Subject, string Body) BuildDelay(Appointment a, int delayMinutes, CultureInfo culture)
        {
            var subject = NotificationText.Get("Subject_Delay", culture);
            // The minutes go bold inside the sentence; the unit word is localized and
            // the surrounding phrase takes it as {0}, keeping HTML out of the resources.
            var minutes = $"<strong>{delayMinutes} {NotificationText.Get("Unit_Minutes", culture)}</strong>";
            var body =
                $"<p>{NotificationText.Format("Greeting", culture, Encode(a.Client.Name))}</p>" +
                $"<p>{NotificationText.Format("Delay_Intro", culture, minutes)}</p>" +
                Details(a, culture) +
                $"<p>{NotificationText.Get("Delay_Outro", culture)}</p>";
            return (subject, body);
        }

        private async Task TrySendPushAsync(string? userId,
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

        private static string PushSummary(Appointment a, CultureInfo culture)
            => $"{a.Service.Name} - {a.StartDate.ToString("g", culture)}";

        private static string Details(Appointment a, CultureInfo culture)
            => "<ul>" +
               $"<li><strong>{NotificationText.Get("Label_Service", culture)}</strong> {Encode(a.Service.Name)}</li>" +
               $"<li><strong>{NotificationText.Get("Label_Professional", culture)}</strong> {Encode(a.Employee.FullName)}</li>" +
               $"<li><strong>{NotificationText.Get("Label_Business", culture)}</strong> {Encode(a.Employee.Business.Name)}</li>" +
               $"<li><strong>{NotificationText.Get("Label_Date", culture)}</strong> {a.StartDate.ToString("g", culture)} - {a.EndDate.ToString("t", culture)}</li>" +
               "</ul>";

        private static CultureInfo ResolveCulture(string? language)
            => CultureInfo.GetCultureInfo(SupportedLanguages.Normalize(language));

        private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
