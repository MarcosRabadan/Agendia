using MRC.Agendia.Application.Statistics;
using MRC.Agendia.Application.Statistics.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Services;
using MRC.Agendia.Domain.Statistics;

namespace MRC.Agendia.Tests.Unit.Application.Statistics
{
    /// <summary>
    /// Unit tests for the pure utilization aggregation. The calendar is a single day
    /// (Monday 2035-06-04) open 09:00-13:00, so every expected number is easy to check by
    /// hand: 240 open minutes per employee.
    /// </summary>
    public class UtilizationCalculatorTests
    {
        private static readonly DateOnly Day = new(2035, 6, 4);
        private static readonly Guid EmployeeA = TestIds.Of(1);
        private static readonly Guid EmployeeB = TestIds.Of(2);

        private static EffectiveSchedule OpenDay(params (TimeOnly Start, TimeOnly End)[] slots) => new()
        {
            Date = Day,
            IsOpen = true,
            TimeSlots = slots.Select(s => new EffectiveTimeSlot { StartTime = s.Start, EndTime = s.End }).ToList()
        };

        private static UtilizationAppointmentRow Appointment(Guid employeeId, TimeOnly start, int minutes)
        {
            var startDate = Day.ToDateTime(start);
            return new UtilizationAppointmentRow(employeeId, startDate, startDate.AddMinutes(minutes), DateTime.UtcNow);
        }

        [Fact]
        public void A_closed_day_offers_nothing_and_reports_no_occupancy()
        {
            var closed = new EffectiveSchedule { Date = Day, IsOpen = false, ClosedReason = "Holiday" };

            var result = Calculate(new[] { closed }, Employees(1), Array.Empty<UtilizationAppointmentRow>());

            Assert.Equal(0, result.OfferedMinutes);
            Assert.Equal(0, result.OccupancyRate);
            Assert.Empty(result.ByHour);
        }

        [Fact]
        public void An_empty_open_day_offers_capacity_with_zero_occupancy()
        {
            var result = Calculate(new[] { OpenDay((new(9, 0), new(13, 0))) }, Employees(1),
                Array.Empty<UtilizationAppointmentRow>());

            Assert.Equal(240, result.OfferedMinutes);
            Assert.Equal(0, result.BookedMinutes);
            Assert.Equal(0, result.OccupancyRate);
            // 09, 10, 11 and 12 are on offer, an hour each.
            Assert.Equal(4, result.ByHour.Count);
            Assert.All(result.ByHour, h => Assert.Equal(60, h.OfferedMinutes));
        }

        [Fact]
        public void A_full_day_reports_complete_occupancy()
        {
            var appointments = new[]
            {
                Appointment(EmployeeA, new(9, 0), 120),
                Appointment(EmployeeA, new(11, 0), 120)
            };

            var result = Calculate(new[] { OpenDay((new(9, 0), new(13, 0))) }, Employees(1), appointments);

            Assert.Equal(240, result.BookedMinutes);
            Assert.Equal(1, result.OccupancyRate);
        }

        [Fact]
        public void Capacity_multiplies_the_open_minutes()
        {
            // One employee able to take two at a time: the same 240 open minutes offer 480.
            var result = Calculate(new[] { OpenDay((new(9, 0), new(13, 0))) },
                new[] { new UtilizationEmployee(EmployeeA, 2) },
                new[] { Appointment(EmployeeA, new(9, 0), 120) });

            Assert.Equal(480, result.OfferedMinutes);
            Assert.Equal(120, result.BookedMinutes);
            Assert.Equal(0.25, result.OccupancyRate);
        }

        [Fact]
        public void Split_shifts_only_offer_the_open_stretches()
        {
            // 09:00-13:00 and 16:00-20:00: the siesta is not on offer.
            var result = Calculate(new[] { OpenDay((new(9, 0), new(13, 0)), (new(16, 0), new(20, 0))) },
                Employees(1), Array.Empty<UtilizationAppointmentRow>());

            Assert.Equal(480, result.OfferedMinutes);
            Assert.DoesNotContain(result.ByHour, h => h.Hour is 13 or 14 or 15);
        }

        [Fact]
        public void An_appointment_across_the_hour_is_split_between_both()
        {
            var result = Calculate(new[] { OpenDay((new(9, 0), new(13, 0))) }, Employees(1),
                new[] { Appointment(EmployeeA, new(9, 45), 45) });

            Assert.Equal(15, result.ByHour.Single(h => h.Hour == 9).BookedMinutes);
            Assert.Equal(30, result.ByHour.Single(h => h.Hour == 10).BookedMinutes);
        }

        [Fact]
        public void Time_off_takes_those_minutes_out_of_the_offer()
        {
            // The employee is away 09:00-11:00, so only half the morning is on offer.
            var block = new EmployeeTimeOff
            {
                EmployeeId = EmployeeA,
                Start = Day.ToDateTime(new TimeOnly(9, 0)),
                End = Day.ToDateTime(new TimeOnly(11, 0))
            };

            var result = Calculate(new[] { OpenDay((new(9, 0), new(13, 0))) }, Employees(1),
                Array.Empty<UtilizationAppointmentRow>(), new[] { block });

            Assert.Equal(120, result.OfferedMinutes);
            Assert.DoesNotContain(result.ByHour, h => h.Hour is 9 or 10);
        }

        [Fact]
        public void Each_employee_gets_their_own_occupancy()
        {
            var result = Calculate(new[] { OpenDay((new(9, 0), new(13, 0))) },
                new[] { new UtilizationEmployee(EmployeeA, 1), new UtilizationEmployee(EmployeeB, 1) },
                new[] { Appointment(EmployeeA, new(9, 0), 120) });

            // 480 offered between the two, 120 booked -> 25% overall, but only A worked.
            Assert.Equal(480, result.OfferedMinutes);
            Assert.Equal(0.25, result.OccupancyRate);
            Assert.Equal(0.5, result.ByEmployee.Single(e => e.EmployeeId == EmployeeA).OccupancyRate);
            Assert.Equal(0, result.ByEmployee.Single(e => e.EmployeeId == EmployeeB).OccupancyRate);
            // Monday is the only weekday with data.
            Assert.Equal(DayOfWeek.Monday, Assert.Single(result.ByWeekday).Weekday);
        }

        [Fact]
        public void Lead_time_is_the_average_of_what_the_caller_measured()
        {
            var result = UtilizationCalculator.Calculate(
                new[] { OpenDay((new(9, 0), new(13, 0))) }, Employees(1),
                Array.Empty<UtilizationAppointmentRow>(), Array.Empty<EmployeeTimeOff>(),
                new[] { 24d, 48d, 12d }, Day, Day);

            Assert.Equal(28d, result.AvgLeadTimeHours);
        }

        private static UtilizationEmployee[] Employees(int capacity) =>
            new[] { new UtilizationEmployee(EmployeeA, capacity) };

        private static UtilizationDto Calculate(
            IReadOnlyList<EffectiveSchedule> schedules,
            IReadOnlyList<UtilizationEmployee> employees,
            IReadOnlyList<UtilizationAppointmentRow> appointments,
            IReadOnlyList<EmployeeTimeOff>? timeOff = null)
            => UtilizationCalculator.Calculate(schedules, employees, appointments,
                timeOff ?? Array.Empty<EmployeeTimeOff>(), Array.Empty<double>(), Day, Day);
    }
}
