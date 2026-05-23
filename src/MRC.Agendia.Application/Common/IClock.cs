namespace MRC.Agendia.Application.Common
{
    /// <summary>
    /// Current time as the business wall-clock (the configured business timezone).
    /// Appointment times are wall-clock values, so the "is it in the past?" check
    /// and the reminder window must compare against this - not against UTC - to
    /// stay coherent regardless of the server's own timezone.
    /// </summary>
    public interface IClock
    {
        /// <summary>Current wall-clock time in the configured business timezone (Kind = Unspecified).</summary>
        DateTime BusinessNow { get; }
    }
}
