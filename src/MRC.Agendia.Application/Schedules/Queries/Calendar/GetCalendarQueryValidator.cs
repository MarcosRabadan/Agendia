using FluentValidation;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Schedules.Queries.Calendar
{
    public class GetCalendarQueryValidator : AbstractValidator<GetCalendarQuery>
    {
        // The calendar is resolved day by day, so bound the range to keep a single
        // request from resolving an unbounded number of days. One year is the
        // natural unit (clients view a year at a time, like the schedule preview).
        private const int MaxRangeDays = 366;

        public GetCalendarQueryValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();

            // Absolute bounds so the day-by-day resolution cannot overflow
            // DateOnly.MaxValue (would be a 500 instead of a 400).
            RuleFor(x => x.From)
                .InclusiveBetween(SchedulingLimits.MinDate, SchedulingLimits.MaxDate)
                .WithMessage(SchedulingLimits.OutOfRangeMessage);
            RuleFor(x => x.To)
                .InclusiveBetween(SchedulingLimits.MinDate, SchedulingLimits.MaxDate)
                .WithMessage(SchedulingLimits.OutOfRangeMessage)
                .GreaterThanOrEqualTo(x => x.From)
                .WithMessage("The end date must be after or equal to the start date.");

            RuleFor(x => x)
                .Must(x => (x.To.DayNumber - x.From.DayNumber) + 1 <= MaxRangeDays)
                .WithMessage($"The calendar range cannot exceed {MaxRangeDays} days.");
        }
    }
}
