using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands.Slots
{
    /// <summary>
    /// Cross-slot rules for a template's weekly slots. The per-slot validator only
    /// checks Start &lt; End; this checks that slots on the SAME weekday do not
    /// overlap. Split shifts may be contiguous (one ends where the next starts) but
    /// must not overlap.
    /// </summary>
    public static class WeeklySlotRules
    {
        public static bool HasIntraDayOverlap(IEnumerable<CreateWeeklyTimeSlotDto>? slots)
        {
            if (slots is null) return false;

            foreach (var sameDay in slots.GroupBy(s => s.DayOfWeek))
                if (HasOverlap(sameDay.Select(s => (s.StartTime, s.EndTime))))
                    return true;

            return false;
        }

        // Override CustomSlots are all for the same override day, so there is no
        // weekday grouping: just check the slots against each other.
        public static bool HasIntraDayOverlap(IEnumerable<CreateCustomTimeSlotDto>? slots)
            => slots is not null && HasOverlap(slots.Select(s => (s.StartTime, s.EndTime)));

        // Sorted by start, any overlap shows up as a slot starting before the previous
        // one ends (this consecutive check detects any overlap, since an out-of-order
        // overlap implies a consecutive one too). Split shifts may be contiguous (one
        // ends exactly where the next starts) but must not overlap.
        private static bool HasOverlap(IEnumerable<(TimeOnly Start, TimeOnly End)> slots)
        {
            var ordered = slots.OrderBy(s => s.Start).ToList();
            for (var i = 1; i < ordered.Count; i++)
                if (ordered[i].Start < ordered[i - 1].End)
                    return true;
            return false;
        }
    }
}
