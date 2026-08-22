namespace MRC.Agendia.Application.Waitlist
{
    /// <summary>
    /// Turns a freed window into the two bounds a queued entry's StartTime must satisfy to
    /// overlap it (#350).
    ///
    /// <para>Lives on its own because the same geometry is needed by both paths that notify -
    /// a cancelled appointment and an expired hold - and this repository has already paid twice
    /// for writing the same rule in two places (#307, #308).</para>
    ///
    /// <para>Every candidate of a given query shares the service, so they all last the same:
    /// the overlap of <c>[S, S+d)</c> with <c>[F1, F2)</c> collapses to
    /// <c>S &lt; F2 &amp;&amp; S &gt; F1 - d</c>, two constants instead of a per-row join.</para>
    /// </summary>
    public static class WaitlistSlotWindow
    {
        /// <summary>
        /// The bounds for the freed window <c>[freedStart, freedEnd)</c> and a queue of entries
        /// lasting <paramref name="serviceDurationMinutes"/>. A null bound means "unbounded on
        /// that side", which is what the day's edges really mean - not a missing filter.
        /// </summary>
        /// <param name="freedStart">Wall-clock start of the freed window.</param>
        /// <param name="freedEnd">Wall-clock end of the freed window (exclusive).</param>
        /// <param name="serviceDurationMinutes">Duration every candidate's slot lasts.</param>
        /// <returns>Exclusive upper bound and exclusive lower bound for a candidate's StartTime.</returns>
        public static (TimeOnly? WindowEnd, TimeOnly? EarliestStart) OverlapBounds(
            DateTime freedStart, DateTime freedEnd, int serviceDurationMinutes)
        {
            // Upper bound: a candidate has to start before the freed window ends. One that runs
            // into the next day bounds nothing on this date, and taking its time-of-day would
            // read as midnight and match nobody.
            TimeOnly? windowEnd = freedEnd.Date > freedStart.Date
                ? null
                : TimeOnly.FromDateTime(freedEnd);

            // Lower bound: a candidate has to be still running when the freed window opens, so it
            // must start later than one duration before it. Closer to midnight than that there is
            // nothing left to exclude - and the subtraction would wrap round to the evening and
            // silently match nobody.
            var start = TimeOnly.FromDateTime(freedStart);
            var duration = Math.Max(0, serviceDurationMinutes);
            TimeOnly? earliestStart = start.ToTimeSpan() >= TimeSpan.FromMinutes(duration)
                ? start.AddMinutes(-duration)
                : null;

            return (windowEnd, earliestStart);
        }
    }
}
