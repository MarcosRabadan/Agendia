using System.Globalization;
using MRC.Agendia.Application.Statistics.DTO;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Statistics;

namespace MRC.Agendia.Application.Statistics
{
    /// <summary>
    /// Pure, in-memory aggregation of the statistics rows. Kept separate from the
    /// data access so it can be unit-tested against a known dataset, and so the
    /// grouping (ISO week, hour, day-of-week) runs in .NET rather than relying on
    /// provider-specific SQL translation.
    /// </summary>
    public static class BusinessStatsCalculator
    {
        public static BusinessStatsDto Calculate(IReadOnlyList<AppointmentStatsRow> rows, DateOnly from, DateOnly to)
        {
            var total = rows.Count;
            var bookings = rows.Where(r => IsBooking(r.Status)).ToList();
            var noShow = rows.Count(r => r.Status == AppointmentStatus.NoShow);
            var cancelled = rows.Count(r => r.Status == AppointmentStatus.Cancelled);
            var revenue = CompletedRevenue(rows);

            var byMonth = bookings
                .GroupBy(r => r.StartDate.ToString("yyyy-MM", CultureInfo.InvariantCulture))
                .Select(g => new PeriodCountDto(g.Key, g.Count()))
                .OrderBy(p => p.Period, StringComparer.Ordinal)
                .ToList();

            var byWeek = bookings
                .GroupBy(r => IsoWeekKey(r.StartDate))
                .Select(g => new PeriodCountDto(g.Key, g.Count()))
                .OrderBy(p => p.Period, StringComparer.Ordinal)
                .ToList();

            // Ranked over bookings, so a service with only no-shows/cancellations
            // does not surface as a 0-count "used service".
            var services = bookings
                .GroupBy(r => new { r.ServiceId, r.ServiceName })
                .Select(g => new ServiceUsageDto(
                    g.Key.ServiceId,
                    g.Key.ServiceName,
                    g.Count(),
                    CompletedRevenue(g)))
                .OrderByDescending(s => s.Count)
                .ThenBy(s => s.ServiceName, StringComparer.Ordinal)
                .ToList();

            var byHour = bookings
                .GroupBy(r => r.StartDate.Hour)
                .Select(g => new HourStatsDto(g.Key, g.Count(), CompletedRevenue(g)))
                .OrderBy(h => h.Hour)
                .ToList();

            var byDayOfWeek = bookings
                .GroupBy(r => r.StartDate.DayOfWeek)
                .Select(g => new DayOfWeekStatsDto(g.Key, g.Count(), CompletedRevenue(g)))
                .OrderBy(d => d.DayOfWeek)
                .ToList();

            return new BusinessStatsDto(
                from, to,
                total,
                bookings.Count,
                revenue,
                noShow, Rate(noShow, total),
                cancelled, Rate(cancelled, total),
                byMonth, byWeek, services, byHour, byDayOfWeek);
        }

        // A "booking" is an appointment that counts as activity: pending, confirmed
        // or completed. NoShow/Cancelled are tracked by their own metrics instead.
        private static bool IsBooking(AppointmentStatus status)
            => status is AppointmentStatus.Pending or AppointmentStatus.Confirmed or AppointmentStatus.Completed;

        // Revenue only counts completed appointments (the visit actually happened).
        private static decimal CompletedRevenue(IEnumerable<AppointmentStatsRow> rows)
            => rows.Where(r => r.Status == AppointmentStatus.Completed).Sum(r => r.ServicePrice);

        private static double Rate(int part, int total)
            => total == 0 ? 0 : Math.Round((double)part / total, 4);

        private static string IsoWeekKey(DateTime date)
            => $"{ISOWeek.GetYear(date)}-W{ISOWeek.GetWeekOfYear(date):00}";
    }
}
