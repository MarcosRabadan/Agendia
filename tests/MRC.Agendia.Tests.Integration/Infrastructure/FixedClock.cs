using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>
    /// An <see cref="IClock"/> pinned to one instant, for the flows whose behaviour depends
    /// on what time it is. Without it a test has to bend around real time - and the ones that
    /// cannot end up skipping themselves, which is a silent hole in the suite rather than a
    /// failure (#310).
    /// </summary>
    public sealed class FixedClock : IClock
    {
        private readonly DateTime _now;

        public FixedClock(DateTime now) => _now = now;

        /// <inheritdoc />
        public DateTime BusinessNow => _now;

        /// <inheritdoc />
        // The tests never cross timezones: the instant is already the wall clock.
        public DateTime ToBusinessTime(DateTime utcInstant) => utcInstant;
    }
}
