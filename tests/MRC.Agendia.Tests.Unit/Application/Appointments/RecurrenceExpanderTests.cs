using MRC.Agendia.Application.Appointments.Recurrence;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Tests.Unit.Application.Appointments
{
    /// <summary>
    /// Unit tests for the pure date expansion that drives recurring series.
    /// Calendar-independent: weekly tests anchor on the start date's own weekday.
    /// </summary>
    public class RecurrenceExpanderTests
    {
        [Fact]
        public void Weekly_SingleDay_EveryWeek_StepsBySevenDays()
        {
            var from = new DateOnly(2030, 1, 7);
            var until = from.AddDays(21); // covers 4 occurrences: day 0, 7, 14, 21

            var result = RecurrenceExpander.Expand(
                RecurrenceFrequency.Weekly, interval: 1,
                daysOfWeek: new[] { from.DayOfWeek }, dayOfMonth: null, from, until);

            Assert.Equal(4, result.Dates.Count);
            Assert.All(result.Dates, d => Assert.Equal(from.DayOfWeek, d.DayOfWeek));
            for (var i = 1; i < result.Dates.Count; i++)
                Assert.Equal(7, result.Dates[i].DayNumber - result.Dates[i - 1].DayNumber);
            Assert.Empty(result.ShortMonths);
        }

        [Fact]
        public void Weekly_Biweekly_StepsByFourteenDays()
        {
            var from = new DateOnly(2030, 1, 7);
            var until = from.AddDays(28); // day 0, 14, 28

            var result = RecurrenceExpander.Expand(
                RecurrenceFrequency.Weekly, interval: 2,
                daysOfWeek: new[] { from.DayOfWeek }, dayOfMonth: null, from, until);

            Assert.Equal(3, result.Dates.Count);
            for (var i = 1; i < result.Dates.Count; i++)
                Assert.Equal(14, result.Dates[i].DayNumber - result.Dates[i - 1].DayNumber);
        }

        [Fact]
        public void Weekly_Biweekly_SingleDay_AnchorsToFirstOccurrence_NotFromsWeek()
        {
            // 'from' is a Saturday; the requested weekday (Sunday) first occurs the next
            // day. A single-day series must start there and step by the interval (same as
            // before the multi-day fix), not skip a whole fortnight.
            var from = new DateOnly(2030, 1, 5); // Saturday
            var until = from.AddDays(20);
            var days = new[] { DayOfWeek.Sunday };

            var result = RecurrenceExpander.Expand(
                RecurrenceFrequency.Weekly, interval: 2, daysOfWeek: days, dayOfMonth: null, from, until);

            Assert.Equal(new[] { new DateOnly(2030, 1, 6), new DateOnly(2030, 1, 20) }, result.Dates);
        }

        [Fact]
        public void Weekly_Biweekly_MultipleDays_KeepsAllDaysInTheSameFortnight()
        {
            // 'from' is a Wednesday. Anchoring each weekday on its own first occurrence
            // used to push Monday to the NEXT week, splitting the fortnight (Mon and Wed
            // landing on alternating weeks). All requested days must stay in the same weeks.
            var from = new DateOnly(2030, 1, 9); // Wednesday
            var until = from.AddDays(27);
            var days = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday };

            var result = RecurrenceExpander.Expand(
                RecurrenceFrequency.Weekly, interval: 2, daysOfWeek: days, dayOfMonth: null, from, until);

            Assert.Equal(
                new[]
                {
                    new DateOnly(2030, 1, 9),   // Wed, base week
                    new DateOnly(2030, 1, 21),  // Mon, two weeks later (same fortnight as 1/23)
                    new DateOnly(2030, 1, 23),  // Wed
                    new DateOnly(2030, 2, 4)    // Mon, next fortnight
                },
                result.Dates);
        }

        [Fact]
        public void Weekly_MultipleDays_ReturnsEachRequestedWeekdaySorted()
        {
            var from = new DateOnly(2030, 1, 1);
            var until = from.AddDays(6); // exactly one full week: each weekday once
            var days = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday };

            var result = RecurrenceExpander.Expand(
                RecurrenceFrequency.Weekly, interval: 1, daysOfWeek: days, dayOfMonth: null, from, until);

            Assert.Equal(3, result.Dates.Count);
            Assert.Equal(days.OrderBy(d => d), result.Dates.Select(d => d.DayOfWeek).OrderBy(d => d));
            // Sorted ascending.
            for (var i = 1; i < result.Dates.Count; i++)
                Assert.True(result.Dates[i] > result.Dates[i - 1]);
        }

        [Fact]
        public void Monthly_DayOfMonth_SkipsAndReportsShortMonths()
        {
            // Day 31 monthly: Jan & Mar have it; Feb (28) & Apr (30) do not.
            var result = RecurrenceExpander.Expand(
                RecurrenceFrequency.Monthly, interval: 1,
                daysOfWeek: null, dayOfMonth: 31,
                from: new DateOnly(2026, 1, 31), until: new DateOnly(2026, 4, 30));

            Assert.Equal(new[] { new DateOnly(2026, 1, 31), new DateOnly(2026, 3, 31) }, result.Dates);
            Assert.Equal(2, result.ShortMonths.Count); // February and April
            Assert.Contains(new DateOnly(2026, 2, 1), result.ShortMonths);
            Assert.Contains(new DateOnly(2026, 4, 1), result.ShortMonths);
        }

        [Fact]
        public void Monthly_Interval2_StepsEveryOtherMonth()
        {
            var result = RecurrenceExpander.Expand(
                RecurrenceFrequency.Monthly, interval: 2,
                daysOfWeek: null, dayOfMonth: 15,
                from: new DateOnly(2026, 1, 10), until: new DateOnly(2026, 6, 30));

            Assert.Equal(
                new[] { new DateOnly(2026, 1, 15), new DateOnly(2026, 3, 15), new DateOnly(2026, 5, 15) },
                result.Dates);
        }

        [Fact]
        public void UntilBeforeFrom_ReturnsEmpty()
        {
            var result = RecurrenceExpander.Expand(
                RecurrenceFrequency.Weekly, interval: 1,
                daysOfWeek: new[] { DayOfWeek.Monday }, dayOfMonth: null,
                from: new DateOnly(2030, 2, 1), until: new DateOnly(2030, 1, 1));

            Assert.Empty(result.Dates);
            Assert.Empty(result.ShortMonths);
        }
    }
}
