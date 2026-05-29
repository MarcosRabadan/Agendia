using MRC.Agendia.Application.Statistics;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Statistics;

namespace MRC.Agendia.Tests.Unit.Application.Statistics
{
    /// <summary>
    /// Unit tests for the pure statistics aggregation against a known dataset.
    /// Calendar-independent: day-of-week assertions read the weekday from the same
    /// dates used to build the rows, and the week split is only checked by invariant.
    /// </summary>
    public class BusinessStatsCalculatorTests
    {
        // Two services; prices: Corte 20, Tinte 50.
        private static readonly DateTime D1 = new(2026, 5, 4, 10, 0, 0);   // booking + completed (Corte)
        private static readonly DateTime D1Eleven = new(2026, 5, 4, 11, 0, 0); // booking confirmed (Corte)
        private static readonly DateTime D2 = new(2026, 5, 5, 10, 0, 0);   // booking + completed (Tinte)
        private static readonly DateTime D3 = new(2026, 5, 6, 16, 0, 0);   // no-show (Corte)
        private static readonly DateTime D4 = new(2026, 5, 7, 9, 0, 0);    // cancelled (Tinte)
        private static readonly DateTime D1Next = new(2026, 5, 11, 10, 0, 0); // booking pending (Corte), same weekday as D1

        private static IReadOnlyList<AppointmentStatsRow> Dataset() => new List<AppointmentStatsRow>
        {
            new(D1, AppointmentStatus.Completed, 1, "Corte", 20m),
            new(D1Eleven, AppointmentStatus.Confirmed, 1, "Corte", 20m),
            new(D2, AppointmentStatus.Completed, 2, "Tinte", 50m),
            new(D3, AppointmentStatus.NoShow, 1, "Corte", 20m),
            new(D4, AppointmentStatus.Cancelled, 2, "Tinte", 50m),
            new(D1Next, AppointmentStatus.Pending, 1, "Corte", 20m),
        };

        [Fact]
        public void Calculate_Totals_AreCorrect()
        {
            var stats = BusinessStatsCalculator.Calculate(Dataset(), new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

            Assert.Equal(6, stats.TotalAppointments);
            Assert.Equal(4, stats.TotalBookings);          // Pending + Confirmed + Completed
            Assert.Equal(70m, stats.TotalRevenue);         // 20 + 50 completed
            Assert.Equal(1, stats.NoShowCount);
            Assert.Equal(1, stats.CancelledCount);
            Assert.Equal(Math.Round(1d / 6, 4), stats.NoShowRate);
            Assert.Equal(Math.Round(1d / 6, 4), stats.CancellationRate);
        }

        [Fact]
        public void Calculate_ByMonth_GroupsBookings()
        {
            var stats = BusinessStatsCalculator.Calculate(Dataset(), new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

            var month = Assert.Single(stats.ByMonth);
            Assert.Equal("2026-05", month.Period);
            Assert.Equal(4, month.Count);
            Assert.Equal(stats.TotalBookings, stats.ByWeek.Sum(w => w.Count)); // invariant
            Assert.All(stats.ByWeek, w => Assert.Matches(@"^\d{4}-W\d{2}$", w.Period)); // ISO week key format
        }

        [Fact]
        public void Calculate_Services_RankedByCount_WithRevenue()
        {
            var stats = BusinessStatsCalculator.Calculate(Dataset(), new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

            Assert.Equal(2, stats.Services.Count);
            // Corte: 3 bookings (completed + confirmed + pending), revenue 20 (one completed).
            Assert.Equal(1, stats.Services[0].ServiceId);
            Assert.Equal(3, stats.Services[0].Count);
            Assert.Equal(20m, stats.Services[0].Revenue);
            // Tinte: 1 booking (completed), revenue 50.
            Assert.Equal(2, stats.Services[1].ServiceId);
            Assert.Equal(1, stats.Services[1].Count);
            Assert.Equal(50m, stats.Services[1].Revenue);
        }

        [Fact]
        public void Calculate_ByHour_CountsBookingsAndCompletedRevenue()
        {
            var stats = BusinessStatsCalculator.Calculate(Dataset(), new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

            var ten = stats.ByHour.Single(h => h.Hour == 10);
            Assert.Equal(3, ten.Count);     // D1 completed, D2 completed, D1Next pending
            Assert.Equal(70m, ten.Revenue); // D1 (20) + D2 (50)

            var eleven = stats.ByHour.Single(h => h.Hour == 11);
            Assert.Equal(1, eleven.Count);
            Assert.Equal(0m, eleven.Revenue);

            // The no-show (16h) and cancelled (9h) are not bookings -> no bucket.
            Assert.DoesNotContain(stats.ByHour, h => h.Hour == 16);
            Assert.DoesNotContain(stats.ByHour, h => h.Hour == 9);
        }

        [Fact]
        public void Calculate_ByDayOfWeek_CountsBookingsAndRevenue()
        {
            var stats = BusinessStatsCalculator.Calculate(Dataset(), new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

            var d1Day = stats.ByDayOfWeek.Single(d => d.DayOfWeek == D1.DayOfWeek);
            Assert.Equal(3, d1Day.Count);   // D1, D1Eleven, D1Next (same weekday)
            Assert.Equal(20m, d1Day.Revenue);

            var d2Day = stats.ByDayOfWeek.Single(d => d.DayOfWeek == D2.DayOfWeek);
            Assert.Equal(1, d2Day.Count);
            Assert.Equal(50m, d2Day.Revenue);
        }

        [Fact]
        public void Calculate_EmptyDataset_ReturnsZeros()
        {
            var stats = BusinessStatsCalculator.Calculate(
                new List<AppointmentStatsRow>(), new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

            Assert.Equal(0, stats.TotalAppointments);
            Assert.Equal(0, stats.TotalBookings);
            Assert.Equal(0m, stats.TotalRevenue);
            Assert.Equal(0d, stats.NoShowRate);
            Assert.Equal(0d, stats.CancellationRate);
            Assert.Empty(stats.ByMonth);
            Assert.Empty(stats.Services);
            Assert.Empty(stats.ByHour);
            Assert.Empty(stats.ByDayOfWeek);
        }
    }
}
