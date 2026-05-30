using MRC.Agendia.Application.Schedules.Commands;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Tests.Unit.Application.Schedules
{
    public class WeeklySlotRulesTests
    {
        private static CreateWeeklyTimeSlotDto Slot(DayOfWeek day, int startHour, int endHour)
            => new(day, new TimeOnly(startHour, 0), new TimeOnly(endHour, 0), default);

        [Fact]
        public void SplitShiftSameDay_NoOverlap_ReturnsFalse()
        {
            var slots = new[] { Slot(DayOfWeek.Monday, 9, 13), Slot(DayOfWeek.Monday, 16, 20) };

            Assert.False(WeeklySlotRules.HasIntraDayOverlap(slots));
        }

        [Fact]
        public void ContiguousSameDay_ReturnsFalse()
        {
            // One ends exactly where the next starts: allowed (not an overlap).
            var slots = new[] { Slot(DayOfWeek.Monday, 9, 13), Slot(DayOfWeek.Monday, 13, 17) };

            Assert.False(WeeklySlotRules.HasIntraDayOverlap(slots));
        }

        [Fact]
        public void OverlapSameDay_ReturnsTrue()
        {
            var slots = new[] { Slot(DayOfWeek.Monday, 9, 14), Slot(DayOfWeek.Monday, 13, 17) };

            Assert.True(WeeklySlotRules.HasIntraDayOverlap(slots));
        }

        [Fact]
        public void SameTimesDifferentDays_ReturnsFalse()
        {
            var slots = new[] { Slot(DayOfWeek.Monday, 9, 17), Slot(DayOfWeek.Tuesday, 9, 17) };

            Assert.False(WeeklySlotRules.HasIntraDayOverlap(slots));
        }
    }
}
