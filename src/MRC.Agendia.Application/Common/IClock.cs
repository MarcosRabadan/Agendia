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

        /// <summary>
        /// IANA id of the business timezone (e.g. <c>Europe/Madrid</c>), as configured.
        ///
        /// <para>It travels in every integration event that carries wall-clock dates (#321):
        /// those are serialized without any zone marker, so a consumer that assumed UTC would
        /// announce the class at the wrong hour, and had no way to correct it because Agendia
        /// never told it which zone the dates were in.</para>
        /// </summary>
        string TimeZoneId { get; }

        /// <summary>
        /// Converts a UTC instant into the business wall clock, so it can be compared with
        /// the appointment dates (which are wall-clock values). Needed wherever the two
        /// meet - the booking lead time subtracts a UTC CreatedAt from a wall-clock start.
        /// </summary>
        /// <param name="utcInstant">A UTC instant (Kind = Utc or Unspecified-as-UTC).</param>
        /// <returns>The same moment expressed in the business timezone.</returns>
        DateTime ToBusinessTime(DateTime utcInstant);
    }
}
