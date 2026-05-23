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
                    $"La zona horaria '{id}' (Scheduling:TimeZone) no existe en este sistema.", ex);
            }
        }

        public DateTime BusinessNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
    }
}
