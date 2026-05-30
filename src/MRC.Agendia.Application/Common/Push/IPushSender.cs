namespace MRC.Agendia.Application.Common.Push
{
    /// <summary>
    /// Push-notification abstraction (mirrors <see cref="Email.IEmailSender"/>).
    /// Implementations live in Infrastructure: a real provider (e.g. FCM) for
    /// delivery, a logging one for dev/test. Kept intentionally small. Callers
    /// treat it as best-effort and swallow failures so a push problem never breaks
    /// the booking flow.
    /// </summary>
    public interface IPushSender
    {
        /// <summary>
        /// Sends a notification to the given device tokens. A null/empty token list
        /// is a no-op. <paramref name="data"/> is an optional key/value payload for
        /// the client app (e.g. an appointmentId for deep-linking).
        /// </summary>
        /// <param name="deviceTokens">Target device tokens; null/empty is a no-op.</param>
        /// <param name="title">Notification title.</param>
        /// <param name="body">Notification body.</param>
        /// <param name="data">Optional key/value payload for the client app (e.g. deep-link ids).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task SendAsync(
            IReadOnlyCollection<string> deviceTokens,
            string title,
            string body,
            IReadOnlyDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default);
    }
}
