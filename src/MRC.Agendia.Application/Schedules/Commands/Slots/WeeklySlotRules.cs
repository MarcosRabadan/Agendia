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
            {
                // Sorted by start, any overlap shows up as a slot starting before the
                // previous one ends (this consecutive check detects any overlap in the
                // group, since an out-of-order overlap implies a consecutive one too).
                var ordered = sameDay.OrderBy(s => s.StartTime).ToList();
                for (var i = 1; i < ordered.Count; i++)
                    if (ordered[i].StartTime < ordered[i - 1].EndTime)
                        return true;
            }

            return false;
        }
    }
}
