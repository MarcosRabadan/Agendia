using Microsoft.Extensions.Configuration;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Infrastructure.Time
{
    /// <summary>
    /// Single, app-wide business timezone (configurable via Scheduling:TimeZone,
    /// default Europe/Madrid). Converts the live UTC instant to wall-clock time in
    /// that zone so "now" lines up with the wall-clock appointment times no matter
    /// what timezone the server runs in.
    /// </summary>
    public class BusinessClock : IClock
    {
        private const string DefaultTimeZoneId = "Europe/Madrid";

        private readonly TimeZoneInfo _timeZone;

        public BusinessClock(IConfiguration configuration)
        {
            var id = configuration["Scheduling:TimeZone"];
            id = string.IsNullOrWhiteSpace(id) ? DefaultTimeZoneId : id;

            try
            {
                _timeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException ex)
            {
                throw new InvalidOperationException(
                    $"The time zone '{id}' (Scheduling:TimeZone) does not exist on this system.", ex);
            }

            // The CONFIGURED id, not _timeZone.Id: on Windows the resolved TimeZoneInfo reports
            // the Windows id ("Romance Standard Time") while on Linux it reports the IANA one,
            // so publishing _timeZone.Id would put a different value in the event payload
            // depending on the host the service happens to run on.
            TimeZoneId = id;
        }

        /// <inheritdoc />
        public string TimeZoneId { get; }

        /// <inheritdoc />
        public DateTime BusinessNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);

        /// <inheritdoc />
        public DateTime ToBusinessTime(DateTime utcInstant)
            // ConvertTimeFromUtc refuses a value already marked Local, so normalise the
            // Kind first: every caller passes an instant it knows to be UTC.
            => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcInstant, DateTimeKind.Utc), _timeZone);
    }
}
