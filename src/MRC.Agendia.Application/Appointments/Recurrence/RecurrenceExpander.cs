using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Appointments.Recurrence
{
    /// <summary>
    /// Pure (no I/O) expansion of a recurrence pattern into the concrete dates it
    /// covers between <c>from</c> and <c>until</c> (both inclusive). Whether each
    /// date is actually bookable (open day, capacity, etc.) is decided later by the
    /// scheduling validator; this only enumerates the candidates.
    /// </summary>
    public static class RecurrenceExpander
    {
        /// <summary>Hard safety cap on the number of generated dates.</summary>
        public const int MaxOccurrences = 366;

        public static RecurrenceExpansion Expand(RecurrenceFrequency frequency,
                                                 int interval,
                                                 IReadOnlyList<DayOfWeek>? daysOfWeek,
                                                 int? dayOfMonth,
                                                 DateOnly from,
                                                 DateOnly until)
        {
            if (interval < 1) interval = 1;

            var dates = new List<DateOnly>();
            var shortMonths = new List<DateOnly>();

            if (until < from)
                return new RecurrenceExpansion(dates, shortMonths);

            switch (frequency)
            {
                case RecurrenceFrequency.Weekly:
                    ExpandWeekly(daysOfWeek, interval, from, until, dates);
                    break;
                case RecurrenceFrequency.Monthly:
                    ExpandMonthly(dayOfMonth, interval, from, until, dates, shortMonths);
                    break;
            }

            dates.Sort();
            if (dates.Count > MaxOccurrences)
                dates = dates.GetRange(0, MaxOccurrences);

            return new RecurrenceExpansion(dates, shortMonths);
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
                    if (date >= from && date <= until)
                        dates.Add(date);
                }
            }
        }

        private static void ExpandMonthly(
            int? dayOfMonth, int interval, DateOnly from, DateOnly until, List<DateOnly> dates, List<DateOnly> shortMonths)
        {
            if (dayOfMonth is null) return;
            var day = dayOfMonth.Value;

            for (var cursor = new DateOnly(from.Year, from.Month, 1); cursor <= until; cursor = cursor.AddMonths(interval))
            {
                var daysInMonth = DateTime.DaysInMonth(cursor.Year, cursor.Month);
                if (day > daysInMonth)
                {
                    // Month has no such day (e.g. 31 in February): report it so the
                    // caller can surface a "skipped" notice to the user.
                    shortMonths.Add(cursor);
                    continue;
                }

                var candidate = new DateOnly(cursor.Year, cursor.Month, day);
                if (candidate >= from && candidate <= until)
                    dates.Add(candidate);
            }
        }
    }
}
