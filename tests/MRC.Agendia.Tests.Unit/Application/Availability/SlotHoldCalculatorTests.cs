using MRC.Agendia.Application.Availability;
using MRC.Agendia.Domain.Availability;

namespace MRC.Agendia.Tests.Unit.Application.Availability
{
    /// <summary>
    /// Unit tests for the pure hold accounting (#308). Everything happens on a single day
    /// (Monday 2035-06-04) around a 10:00-11:00 candidate window, so each expectation is
    /// checkable by hand. The point of these is that a hold is ONE seat for ONE client:
    /// the availability read, the capacity probe and the scheduling validator all measure
    /// it through here, so they cannot contradict each other again.
    /// </summary>
    public class SlotHoldCalculatorTests
    {
        private static readonly DateOnly Day = new(2035, 6, 4);
        private static readonly Guid EmployeeA = TestIds.Of(1);
        private static readonly Guid EmployeeB = TestIds.Of(2);

        private const string Holder = "holder-sub";
        private const string Someone = "someone-else-sub";

        private static SlotHold Hold(string clientUserId, Guid? employeeId, TimeOnly start, int minutes)
        {
            var startDate = Day.ToDateTime(start);
            return new SlotHold(clientUserId, employeeId, startDate, startDate.AddMinutes(minutes));
        }

        private static HeldSeats Count(IReadOnlyList<SlotHold> holds,
                                       string? requester = Someone,
                                       TimeOnly? windowStart = null,
                                       int windowMinutes = 60)
        {
            var start = Day.ToDateTime(windowStart ?? new TimeOnly(10, 0));
            return SlotHoldCalculator.Count(holds, requester, start, start.AddMinutes(windowMinutes));
        }

        [Fact]
        public void No_holds_means_nothing_is_held()
        {
            var held = Count(Array.Empty<SlotHold>());

            Assert.Equal(0, held.AnyEmployee);
            Assert.Equal(0, held.For(EmployeeA));
        }

        [Fact]
        public void The_requesters_own_hold_does_not_count_against_them()
        {
            // Otherwise the client could not book the slot they were just offered.
            var held = Count(new[] { Hold(Someone, EmployeeA, new TimeOnly(10, 0), 60) });

            Assert.Equal(0, held.For(EmployeeA));
            Assert.Equal(0, held.AnyEmployee);
        }

        [Fact]
        public void A_hold_naming_an_employee_costs_only_that_employee_a_seat()
        {
            // The bug this replaces dropped the employee entirely, so a group class of 3
            // with one hold reported as full.
            var held = Count(new[] { Hold(Holder, EmployeeA, new TimeOnly(10, 0), 60) });

            Assert.Equal(1, held.For(EmployeeA));
            Assert.Equal(0, held.For(EmployeeB));
            Assert.Equal(0, held.AnyEmployee);
        }

        [Fact]
        public void An_any_employee_hold_costs_the_business_a_seat_and_no_employee_in_particular()
        {
            // The bug this replaces counted it against EVERY employee, so one held seat
            // rejected all of them with SLOT_ON_HOLD.
            var held = Count(new[] { Hold(Holder, null, new TimeOnly(10, 0), 60) });

            Assert.Equal(1, held.AnyEmployee);
            Assert.Equal(0, held.For(EmployeeA));
            Assert.Equal(0, held.For(EmployeeB));
        }

        [Fact]
        public void Several_holds_on_one_employee_stack()
        {
            var held = Count(new[]
            {
                Hold(Holder, EmployeeA, new TimeOnly(10, 0), 60),
                Hold("third-sub", EmployeeA, new TimeOnly(10, 0), 60)
            });

            Assert.Equal(2, held.For(EmployeeA));
        }

        [Fact]
        public void A_null_requester_sees_every_hold()
        {
            // The background jobs hold nothing, so nothing may be excluded for them.
            var held = Count(
                new[] { Hold(Holder, EmployeeA, new TimeOnly(10, 0), 60) },
                requester: null);

            Assert.Equal(1, held.For(EmployeeA));
        }

        [Fact]
        public void A_hold_protects_its_window_not_just_its_start_time()
        {
            // A 10:00 hold used to be bookable over by 09:45-10:15, because only the start
            // times were compared.
            var held = Count(
                new[] { Hold(Holder, EmployeeA, new TimeOnly(10, 0), 60) },
                windowStart: new TimeOnly(9, 45),
                windowMinutes: 30);

            Assert.Equal(1, held.For(EmployeeA));
        }

        [Fact]
        public void A_hold_that_ends_when_the_window_opens_does_not_overlap_it()
        {
            // Half-open [Start, End), the same convention the rest of the agenda uses.
            var held = Count(
                new[] { Hold(Holder, EmployeeA, new TimeOnly(9, 0), 60) },
                windowStart: new TimeOnly(10, 0));

            Assert.Equal(0, held.For(EmployeeA));
        }

        [Fact]
        public void A_hold_that_starts_when_the_window_closes_does_not_overlap_it()
        {
            var held = Count(
                new[] { Hold(Holder, EmployeeA, new TimeOnly(11, 0), 60) },
                windowStart: new TimeOnly(10, 0));

            Assert.Equal(0, held.For(EmployeeA));
        }

        [Fact]
        public void A_shorter_hold_inside_the_window_still_counts()
        {
            // The held window is the holder's own service duration, which need not match
            // the duration being booked over it.
            var held = Count(
                new[] { Hold(Holder, EmployeeA, new TimeOnly(10, 15), 30) },
                windowStart: new TimeOnly(10, 0));

            Assert.Equal(1, held.For(EmployeeA));
        }

        [Fact]
        public void The_two_levels_are_counted_apart_so_no_seat_is_discounted_twice()
        {
            var held = Count(new[]
            {
                Hold(Holder, EmployeeA, new TimeOnly(10, 0), 60),
                Hold("third-sub", null, new TimeOnly(10, 0), 60)
            });

            Assert.Equal(1, held.For(EmployeeA));
            Assert.Equal(1, held.AnyEmployee);
        }
    }
}
