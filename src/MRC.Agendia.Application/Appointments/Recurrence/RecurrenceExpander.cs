using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Appointments.Recurrence
{
    /// <summary>
    /// Pure (no I/O) expansion of a recurrence pattern into the concrete dates it
    /// covers between <c>from</c> and <c>until</c> (both inclusive). Whether each
    /// date is actually bookable (open day, capacity, etc.) is decided later by the
    /// scheduling validator; this only enumerates the candidates and reports the ones
    /// the calendar itself rules out.
    /// </summary>
    public static class RecurrenceExpander
    {
        /// <summary>Hard safety cap on the number of generated dates.</summary>
        public const int MaxOccurrences = 366;

        /// <summary>The month has no such day of the month (e.g. the 31st in February).</summary>
        public const string MonthWithoutDayCode = "RECURRENCE_MONTH_WITHOUT_DAY";

        /// <summary>The pattern produced more dates than <see cref="MaxOccurrences"/>.</summary>
        public const string LimitReachedCode = "RECURRENCE_LIMIT_REACHED";

        /// <summary>That month's requested day had already passed when the series starts.</summary>
        public const string DayAlreadyPassedCode = "RECURRENCE_DAY_ALREADY_PASSED";

        public static RecurrenceExpansion Expand(RecurrenceFrequency frequency,
                                                 int interval,
                                                 IReadOnlyList<DayOfWeek>? daysOfWeek,
                                                 int? dayOfMonth,
                                                 DateOnly from,
                                                 DateOnly until)
        {
            if (interval < 1) interval = 1;

            var dates = new List<DateOnly>();
            var skipped = new List<SkippedOccurrenceDto>();

            if (until < from)
                return new RecurrenceExpansion(dates, skipped);

            switch (frequency)
            {
                case RecurrenceFrequency.Weekly:
                    ExpandWeekly(daysOfWeek, interval, from, until, dates);
                    break;
                case RecurrenceFrequency.Monthly:
                    ExpandMonthly(dayOfMonth, interval, from, until, dates, skipped);
                    break;
            }

            dates.Sort();

            // Anything past the cap is REPORTED, not silently trimmed: those are classes the
            // caller asked for, and staff has to learn they were not booked.
            if (dates.Count > MaxOccurrences)
            {
                foreach (var dropped in dates.GetRange(MaxOccurrences, dates.Count - MaxOccurrences))
                {
                    skipped.Add(new SkippedOccurrenceDto(dropped, LimitReachedCode,
                        $"The series exceeds the limit of {MaxOccurrences} occurrences."));
                }

                dates = dates.GetRange(0, MaxOccurrences);
            }

            return new RecurrenceExpansion(dates, skipped);
        }

        private static void ExpandWeekly(
            IReadOnlyList<DayOfWeek>? daysOfWeek, int interval, DateOnly from, DateOnly until, List<DateOnly> dates)
        {
            if (daysOfWeek is null) return;

            var days = daysOfWeek.Distinct().ToList();
            if (days.Count == 0) return;

            // Anchor the whole pattern to the EARLIEST first occurrence (on/after 'from')
            // among the requested weekdays, then step that base week by 'interval'. This
            // keeps a multi-day biweekly pattern's days together in the same fortnight
            // (anchoring each weekday on its own first occurrence let them drift to their
            // own weeks), while a single weekday still starts at its own first occurrence.
            var anchor = days.Min(day => from.AddDays(((int)day - (int)from.DayOfWeek + 7) % 7));
            var weekStart = anchor.AddDays(-(int)anchor.DayOfWeek); // Sunday of the anchor's week
            for (var ws = weekStart; ws <= until; ws = ws.AddDays(7 * interval))
            {
                foreach (var day in days)
                {
                    var date = ws.AddDays((int)day);
                    // Dates of the anchor week that precede 'from' are not reported: they fall
                    // outside the window the caller asked for, so nothing was lost.
                    if (date >= from && date <= until)
                        dates.Add(date);
                }
            }
        }

        private static void ExpandMonthly(int? dayOfMonth,
                                          int interval,
                                          DateOnly from,
                                          DateOnly until,
                                          List<DateOnly> dates,
                                          List<SkippedOccurrenceDto> skipped)
        {
            if (dayOfMonth is null) return;
            var day = dayOfMonth.Value;

            for (var cursor = new DateOnly(from.Year, from.Month, 1); cursor <= until; cursor = cursor.AddMonths(interval))
            {
                var daysInMonth = DateTime.DaysInMonth(cursor.Year, cursor.Month);
                if (day > daysInMonth)
                {
                    // Month has no such day (e.g. 31 in February): reported so the caller can
                    // show it, keyed by the first of that month.
                    skipped.Add(new SkippedOccurrenceDto(cursor, MonthWithoutDayCode,
                        $"Month {cursor:yyyy-MM} does not have day {day}."));
                    continue;
                }

                var candidate = new DateOnly(cursor.Year, cursor.Month, day);

                // Past the requested end: outside the window, so nothing was lost.
                if (candidate > until)
                    continue;

                // The first month's day may already have gone by when the series starts.
                // Reported rather than dropped: otherwise a series that yields nothing comes
                // back with no dates AND no explanation.
                if (candidate < from)
                {
                    skipped.Add(new SkippedOccurrenceDto(candidate, DayAlreadyPassedCode,
                        $"Day {day} of {candidate:yyyy-MM} falls before the start of the series."));
                    continue;
                }

                dates.Add(candidate);
            }
        }
    }
}
