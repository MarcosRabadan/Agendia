using MRC.Agendia.Application.Statistics.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Services;
using MRC.Agendia.Domain.Statistics;

namespace MRC.Agendia.Application.Statistics
{
    /// <summary>
    /// Pure, in-memory aggregation of the agenda utilization. Kept apart from the data
    /// access so the arithmetic can be unit-tested against a known calendar (same split as
    /// <see cref="BusinessStatsCalculator"/>).
    ///
    /// <para><b>The unit is a minute of agenda.</b> A day that opens 09:00-13:00 with two
    /// employees offers 480 minutes; one of them being off all morning drops it to 240.
    /// Measuring in minutes rather than in "slots" avoids inventing a slot length that
    /// nothing else in the system uses, and it copes with appointments of any duration.</para>
    ///
    /// <para>Everything is bucketed per hour, per weekday and per employee in a single pass
    /// over the days, because the same open range feeds all three views.</para>
    /// </summary>
    public static class UtilizationCalculator
    {
        public static UtilizationDto Calculate(IReadOnlyList<EffectiveSchedule> schedules,
                                               IReadOnlyList<UtilizationEmployee> employees,
                                               IReadOnlyList<UtilizationAppointmentRow> appointments,
                                               IReadOnlyList<EmployeeTimeOff> timeOff,
                                               IReadOnlyList<double> leadTimesHours,
                                               DateOnly from,
                                               DateOnly to)
        {
            var offeredByHour = new int[24];
            var bookedByHour = new int[24];
            var offeredByWeekday = new Dictionary<DayOfWeek, int>();
            var bookedByWeekday = new Dictionary<DayOfWeek, int>();
            var offeredByEmployee = employees.ToDictionary(e => e.Id, _ => 0);
            var bookedByEmployee = employees.ToDictionary(e => e.Id, _ => 0);

            var timeOffByEmployee = timeOff
                .GroupBy(t => t.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // ---------- Offered capacity, from the effective schedule ----------
            foreach (var day in schedules.Where(s => s.IsOpen))
            {
                foreach (var employee in employees)
                {
                    var blocks = timeOffByEmployee.TryGetValue(employee.Id, out var b) ? b : new List<EmployeeTimeOff>();

                    foreach (var slot in day.TimeSlots)
                    {
                        var slotStart = day.Date.ToDateTime(slot.StartTime);
                        var slotEnd = day.Date.ToDateTime(slot.EndTime);

                        // Walk the slot hour by hour so the per-hour view is exact even for
                        // a shift that starts or ends mid-hour.
                        foreach (var (hour, minutes) in SplitByHour(slotStart, slotEnd))
                        {
                            var open = minutes - BlockedMinutes(blocks, day.Date, hour, slotStart, slotEnd);
                            if (open <= 0)
                                continue;

                            var offered = open * employee.MaxConcurrentAppointments;
                            offeredByHour[hour] += offered;
                            Add(offeredByWeekday, day.Date.DayOfWeek, offered);
                            offeredByEmployee[employee.Id] += offered;
                        }
                    }
                }
            }

            // ---------- Booked minutes, from the appointments ----------
            foreach (var appointment in appointments)
            {
                var weekday = DateOnly.FromDateTime(appointment.StartDate).DayOfWeek;

                foreach (var (hour, minutes) in SplitByHour(appointment.StartDate, appointment.EndDate))
                {
                    bookedByHour[hour] += minutes;
                    Add(bookedByWeekday, weekday, minutes);
                    if (bookedByEmployee.ContainsKey(appointment.EmployeeId))
                        bookedByEmployee[appointment.EmployeeId] += minutes;
                }
            }

            var totalOffered = offeredByHour.Sum();
            var totalBooked = bookedByHour.Sum();

            return new UtilizationDto(
                from,
                to,
                totalOffered,
                totalBooked,
                Rate(totalBooked, totalOffered),
                leadTimesHours.Count == 0 ? 0 : Math.Round(leadTimesHours.Average(), 2),
                Enumerable.Range(0, 24)
                    .Where(h => offeredByHour[h] > 0 || bookedByHour[h] > 0)
                    .Select(h => new HourUtilizationDto(h, offeredByHour[h], bookedByHour[h], Rate(bookedByHour[h], offeredByHour[h])))
                    .ToList(),
                offeredByWeekday.Keys.Union(bookedByWeekday.Keys)
                    .OrderBy(d => d)
                    .Select(d => new WeekdayUtilizationDto(
                        d, Value(offeredByWeekday, d), Value(bookedByWeekday, d),
                        Rate(Value(bookedByWeekday, d), Value(offeredByWeekday, d))))
                    .ToList(),
                employees
                    .Select(e => new EmployeeUtilizationDto(
                        e.Id, offeredByEmployee[e.Id], bookedByEmployee[e.Id],
                        Rate(bookedByEmployee[e.Id], offeredByEmployee[e.Id])))
                    .OrderByDescending(e => e.OccupancyRate)
                    .ToList());
        }

        /// <summary>
        /// Splits a wall-clock range into (hour of day, minutes) pieces. A 09:45-10:30
        /// appointment yields 15 minutes on hour 9 and 30 on hour 10.
        /// </summary>
        private static IEnumerable<(int Hour, int Minutes)> SplitByHour(DateTime start, DateTime end)
        {
            var cursor = start;
            while (cursor < end)
            {
                var nextHour = cursor.Date.AddHours(cursor.Hour + 1);
                var pieceEnd = nextHour < end ? nextHour : end;
                var minutes = (int)Math.Round((pieceEnd - cursor).TotalMinutes);
                if (minutes > 0)
                    yield return (cursor.Hour, minutes);
                cursor = pieceEnd;
            }
        }

        // Minutes of a given hour of the open slot the employee is away for.
        private static int BlockedMinutes(List<EmployeeTimeOff> blocks,
                                          DateOnly date,
                                          int hour,
                                          DateTime slotStart,
                                          DateTime slotEnd)
        {
            if (blocks.Count == 0)
                return 0;

            // The piece of the slot that falls inside this hour.
            var hourStart = date.ToDateTime(TimeOnly.MinValue).AddHours(hour);
            var pieceStart = Max(hourStart, slotStart);
            var pieceEnd = Min(hourStart.AddHours(1), slotEnd);
            if (pieceEnd <= pieceStart)
                return 0;

            // Blocks of one employee do not overlap in practice; if they did, the overlap
            // would be counted twice, which only understates the offered capacity.
            return blocks.Sum(block =>
            {
                var overlapStart = Max(pieceStart, block.Start);
                var overlapEnd = Min(pieceEnd, block.End);
                return overlapEnd > overlapStart ? (int)Math.Round((overlapEnd - overlapStart).TotalMinutes) : 0;
            });
        }

        private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

        private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

        private static void Add(Dictionary<DayOfWeek, int> target, DayOfWeek key, int value)
            => target[key] = target.TryGetValue(key, out var current) ? current + value : value;

        private static int Value(Dictionary<DayOfWeek, int> source, DayOfWeek key)
            => source.TryGetValue(key, out var value) ? value : 0;

        // Rounded so the payload does not leak binary-fraction noise, and capped at 1:
        // an overbooked slot (capacity raised and then lowered) must not report 130%.
        private static double Rate(int booked, int offered)
            => offered <= 0 ? 0 : Math.Round(Math.Min(1d, (double)booked / offered), 4);
    }
}
