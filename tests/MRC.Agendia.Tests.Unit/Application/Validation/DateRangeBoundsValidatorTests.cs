using FluentValidation.Results;
using MRC.Agendia.Application.Appointments.Commands.Series;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Statistics.Queries;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Tests.Unit.Application.Validation
{
    /// <summary>
    /// Guards the fix for the DateOnly.AddDays overflow: the date-range validators
    /// reject dates outside [MinDate, MaxDate], so a value at DateOnly.MaxValue can
    /// no longer slip past a size-only range check and crash the handler (500)
    /// during the downstream day maths. It must return a clean 400 instead.
    /// </summary>
    public class DateRangeBoundsValidatorTests
    {
        private static readonly DateOnly Valid = new(2026, 6, 1);

        [Fact]
        public void Stats_rejects_a_date_at_DateOnly_MaxValue()
        {
            var validator = new GetBusinessStatsQueryValidator();

            // From == To == MaxValue passes the size-only cap (range = 1 day) but must
            // now fail the absolute bound instead of overflowing To.AddDays(1).
            var result = validator.Validate(new GetBusinessStatsQuery(TestIds.Of(1), DateOnly.MaxValue, DateOnly.MaxValue));

            AssertOutOfRange(result);
        }

        [Fact]
        public void Stats_accepts_a_normal_range()
        {
            var validator = new GetBusinessStatsQueryValidator();

            var result = validator.Validate(new GetBusinessStatsQuery(TestIds.Of(1), Valid, Valid.AddDays(30)));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Series_rejects_a_start_date_at_DateOnly_MaxValue()
        {
            var validator = new CreateAppointmentSeriesCommandValidator();
            var dto = new CreateAppointmentSeriesDto(
                ClientUserId: "client-1",
                EmployeeId: TestIds.Of(1),
                ServiceId: TestIds.Of(1),
                StartTime: new TimeOnly(10, 0),
                Frequency: RecurrenceFrequency.Weekly,
                Interval: 1,
                DaysOfWeek: new[] { DayOfWeek.Monday },
                DayOfMonth: null,
                StartDate: DateOnly.MaxValue,
                UntilDate: DateOnly.MaxValue,
                Notes: null);

            var result = validator.Validate(new CreateAppointmentSeriesCommand(dto));

            AssertOutOfRange(result);
        }

        private static void AssertOutOfRange(ValidationResult result)
        {
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage == SchedulingLimits.OutOfRangeMessage);
        }
    }
}
