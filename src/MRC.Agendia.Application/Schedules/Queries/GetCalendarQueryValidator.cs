using FluentValidation;

namespace MRC.Agendia.Application.Schedules.Queries
{
    public class GetCalendarQueryValidator : AbstractValidator<GetCalendarQuery>
    {
        // The calendar is resolved day by day, so bound the range to keep a single
        // request from resolving an unbounded number of days. One year is the
        // natural unit (clients view a year at a time, like the schedule preview).
        private const int MaxRangeDays = 366;

        public GetCalendarQueryValidator()
        {
            RuleFor(x => x.BusinessId).GreaterThan(0);

            RuleFor(x => x.To)
                .GreaterThanOrEqualTo(x => x.From)
                .WithMessage("La fecha final debe ser posterior o igual a la inicial.");

            RuleFor(x => x)
                .Must(x => x.To.DayNumber - x.From.DayNumber <= MaxRangeDays)
                .WithMessage($"El rango del calendario no puede superar {MaxRangeDays} dias.");
        }
    }
}
